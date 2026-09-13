using System.Collections.Concurrent;
using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Application.Contracts;
using Fundo.Loans.Domain.Entities;
using Fundo.Loans.Infrastructure.Outbox;
using Fundo.Loans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Fundo.Loans.IntegrationTests.Outbox;

/// <summary>The dispatcher against real Postgres: atomic lease-based claiming, delivery and retry scheduling.</summary>
public sealed class OutboxDispatcherTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fixture;

    public OutboxDispatcherTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await using var dbContext = _fixture.CreateDbContext();
        await dbContext.OutboxMessages.ExecuteDeleteAsync(Ct);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task TwoConcurrentDispatchers_DeliverEveryMessageExactlyOnce()
    {
        const int messageCount = 30;
        await EnqueueAsync(messageCount);
        var registry = new RecordingRegistry(delay: TimeSpan.FromMilliseconds(20));

        var first = BuildDispatcher(registry, batchSize: 10);
        var second = BuildDispatcher(registry, batchSize: 10);

        for (var round = 0; round < 4; round++)
        {
            await Task.WhenAll(first.DispatchBatchAsync(Ct), second.DispatchBatchAsync(Ct));
        }

        Assert.Equal(messageCount, registry.Deliveries.Count);
        Assert.Equal(messageCount, registry.Deliveries.Select(d => d.IdempotencyKey).Distinct().Count());

        await using var verify = _fixture.CreateDbContext();
        Assert.Equal(messageCount, await verify.OutboxMessages.CountAsync(m => m.ProcessedAtUtc != null, Ct));
    }

    [Fact]
    public async Task FailedDelivery_IsScheduledForRetryWithBackoffInsteadOfBeingMarkedProcessed()
    {
        await EnqueueAsync(1);
        var before = DateTime.UtcNow;

        await BuildDispatcher(new FailingRegistry(), batchSize: 10).DispatchBatchAsync(Ct);

        await using var verify = _fixture.CreateDbContext();
        var message = await verify.OutboxMessages.SingleAsync(Ct);
        Assert.Null(message.ProcessedAtUtc);
        Assert.Equal(1, message.Attempts);
        Assert.Equal("external service down", message.LastError);
        Assert.True(message.NextAttemptAtUtc >= before.AddSeconds(2));
        Assert.True(message.NextAttemptAtUtc < before.AddSeconds(60));
    }

    [Fact]
    public async Task ClaimedButUndeliveredMessage_IsNotDueAgainUntilItsLeaseExpires()
    {
        await EnqueueAsync(1);
        var blocking = new BlockingRegistry();
        var recording = new RecordingRegistry(delay: TimeSpan.Zero);

        // The first dispatcher claims the message and is still "on the wire" when the second polls.
        var inFlight = BuildDispatcher(blocking, batchSize: 10).DispatchBatchAsync(Ct);
        await blocking.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);

        await BuildDispatcher(recording, batchSize: 10).DispatchBatchAsync(Ct);
        blocking.Release.SetResult();
        await inFlight;

        Assert.Empty(recording.Deliveries);
        await using var verify = _fixture.CreateDbContext();
        Assert.NotNull((await verify.OutboxMessages.SingleAsync(Ct)).ProcessedAtUtc);
    }

    private async Task EnqueueAsync(int count)
    {
        await using var dbContext = _fixture.CreateDbContext();

        for (var i = 0; i < count; i++)
        {
            var payload = new CustomerUpsertPayload(
                new CustomerPayload(Guid.CreateVersion7(), "Jane", "Doe", "Acme", "5555", new AddressPayload("1 Main St", "Springfield", "CA", "94105")),
                new LoanApplicationPayload(Guid.CreateVersion7(), 10_000m, "USD", "Approved"),
                "create",
                DateTime.UtcNow);

            dbContext.OutboxMessages.Add(OutboxMessage.For(
                "customer.upserted",
                System.Text.Json.JsonSerializer.Serialize(payload, OutboxJson.Options),
                DateTime.UtcNow.AddSeconds(-1)));
        }

        await dbContext.SaveChangesAsync(Ct);
    }

    private OutboxDispatcher BuildDispatcher(IExternalCustomerRegistry registry, int batchSize)
    {
        var services = new ServiceCollection();
        services.AddDbContext<LoansDbContext>(options => options.UseNpgsql(_fixture.ConnectionString));
        services.AddSingleton(registry);
        var provider = services.BuildServiceProvider();

        return new OutboxDispatcher(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxOptions { BatchSize = batchSize, MaxAttempts = 5 }),
            TimeProvider.System,
            NullLogger<OutboxDispatcher>.Instance);
    }

    private sealed class RecordingRegistry(TimeSpan delay) : IExternalCustomerRegistry
    {
        public ConcurrentBag<(Guid CustomerId, string IdempotencyKey)> Deliveries { get; } = [];

        public async Task UpsertAsync(CustomerUpsertPayload payload, string idempotencyKey, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            Deliveries.Add((payload.Customer.Id, idempotencyKey));
        }
    }

    private sealed class BlockingRegistry : IExternalCustomerRegistry
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task UpsertAsync(CustomerUpsertPayload payload, string idempotencyKey, CancellationToken cancellationToken)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class FailingRegistry : IExternalCustomerRegistry
    {
        public Task UpsertAsync(CustomerUpsertPayload payload, string idempotencyKey, CancellationToken cancellationToken)
            => throw new HttpRequestException("external service down");
    }
}
