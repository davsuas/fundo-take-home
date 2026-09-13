namespace Fundo.Loans.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    public int BatchSize { get; set; } = 10;

    public int MaxAttempts { get; set; } = 5;

    /// <summary>Cap on the exponential backoff between retries.</summary>
    public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// How long a claimed message is hidden from other dispatchers. Must exceed the worst-case
    /// time to deliver one batch (BatchSize x HTTP timeout); if a worker dies mid-batch, its
    /// messages become due again once the lease expires.
    /// </summary>
    public TimeSpan ClaimLease { get; set; } = TimeSpan.FromMinutes(2);
}
