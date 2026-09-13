namespace Fundo.Loans.Domain.Entities;

/// <summary>
/// A durable record of "this event must be delivered to the external service". Written in the
/// same transaction as the customer/application upsert and delivered later by the worker, so an
/// event exists if and only if the business data it describes was committed.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; }
    public string Payload { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public int Attempts { get; private set; }
    public DateTime NextAttemptAtUtc { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? DeadLetteredAtUtc { get; private set; }

    private OutboxMessage(Guid id, string type, string payload, DateTime occurredAtUtc)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
        NextAttemptAtUtc = occurredAtUtc;
    }

#pragma warning disable CS8618 // EF Core materialization constructor.
    private OutboxMessage()
    {
    }
#pragma warning restore CS8618

    public static OutboxMessage For(string type, string payload, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new OutboxMessage(Guid.CreateVersion7(), type, payload, nowUtc);
    }

    public void MarkProcessed(DateTime nowUtc)
    {
        ProcessedAtUtc = nowUtc;
    }

    /// <summary>
    /// Schedules the next attempt with exponential backoff (2^attempts seconds, capped at
    /// <paramref name="maxBackoff"/>) and dead-letters the message once
    /// <paramref name="maxAttempts"/> is reached, so a permanently failing event stops retrying.
    /// </summary>
    public void RecordFailure(string error, DateTime nowUtc, int maxAttempts, TimeSpan maxBackoff)
    {
        Attempts++;
        LastError = error.Length > 2000 ? error[..2000] : error;

        var backoffSeconds = Math.Min(Math.Pow(2, Attempts), maxBackoff.TotalSeconds);
        NextAttemptAtUtc = nowUtc.AddSeconds(backoffSeconds);

        if (Attempts >= maxAttempts)
        {
            DeadLetteredAtUtc = nowUtc;
        }
    }
}
