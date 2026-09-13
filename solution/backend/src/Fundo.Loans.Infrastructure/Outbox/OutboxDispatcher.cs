using System.Text.Json;
using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Application.Contracts;
using Fundo.Loans.Domain.Entities;
using Fundo.Loans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fundo.Loans.Infrastructure.Outbox;

/// <summary>
/// Polls <c>outbox_messages</c> and delivers due messages to the external service.
/// A batch is claimed with one atomic <c>UPDATE ... FOR UPDATE SKIP LOCKED ... RETURNING</c> that
/// pushes <c>next_attempt_at</c> forward by <see cref="OutboxOptions.ClaimLease"/>, so no database
/// lock is held while the HTTP calls run and several workers can dispatch concurrently without
/// claiming the same row.
/// </summary>
public sealed class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        TimeProvider timeProvider,
        ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.PollInterval, _timeProvider);

        do
        {
            try
            {
                await DispatchBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "OutboxDispatchBatchFailed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    /// <summary>Claims and delivers one batch. Public so integration tests can drive it without waiting on the poll timer.</summary>
    public async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LoansDbContext>();
        var registry = scope.ServiceProvider.GetRequiredService<IExternalCustomerRegistry>();

        var messages = await ClaimDueMessagesAsync(dbContext, cancellationToken).ConfigureAwait(false);

        foreach (var message in messages)
        {
            await DeliverAsync(message, registry, cancellationToken).ConfigureAwait(false);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private Task<List<OutboxMessage>> ClaimDueMessagesAsync(LoansDbContext dbContext, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var leaseUntil = now.Add(_options.ClaimLease);

        return dbContext.OutboxMessages
            .FromSqlInterpolated($"""
                UPDATE outbox_messages SET next_attempt_at = {leaseUntil}
                WHERE id IN (
                    SELECT id FROM outbox_messages
                    WHERE processed_at IS NULL AND dead_lettered_at IS NULL AND next_attempt_at <= {now}
                    ORDER BY occurred_at
                    LIMIT {_options.BatchSize}
                    FOR UPDATE SKIP LOCKED)
                RETURNING *
                """)
            .AsTracking()
            .ToListAsync(cancellationToken);
    }

    private async Task DeliverAsync(OutboxMessage message, IExternalCustomerRegistry registry, CancellationToken cancellationToken)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<CustomerUpsertPayload>(message.Payload, OutboxJson.Options)
                ?? throw new InvalidOperationException($"Outbox message {message.Id} has an unreadable payload.");

            await registry.UpsertAsync(payload, message.Id.ToString(), cancellationToken).ConfigureAwait(false);

            message.MarkProcessed(_timeProvider.GetUtcNow().UtcDateTime);
            _logger.LogInformation(
                "OutboxMessageDelivered messageId={MessageId} customerId={CustomerId} operation={Operation}",
                message.Id,
                payload.Customer.Id,
                payload.Operation);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            message.RecordFailure(ex.Message, _timeProvider.GetUtcNow().UtcDateTime, _options.MaxAttempts, _options.MaxBackoff);
            _logger.LogWarning(
                ex,
                "OutboxDeliveryFailed messageId={MessageId} attempts={Attempts} deadLettered={DeadLettered}",
                message.Id,
                message.Attempts,
                message.DeadLetteredAtUtc is not null);
        }
    }
}
