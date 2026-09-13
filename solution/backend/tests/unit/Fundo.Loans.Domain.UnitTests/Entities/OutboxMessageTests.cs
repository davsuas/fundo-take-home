using Fundo.Loans.Domain.Entities;
using Xunit;

namespace Fundo.Loans.Domain.UnitTests.Entities;

public class OutboxMessageTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(60);

    [Fact]
    public void For_CreatesPendingMessageDueImmediately()
    {
        var message = OutboxMessage.For("customer.upserted", "{}", Now);

        Assert.Equal("customer.upserted", message.Type);
        Assert.Equal(Now, message.OccurredAtUtc);
        Assert.Equal(Now, message.NextAttemptAtUtc);
        Assert.Null(message.ProcessedAtUtc);
        Assert.Equal(0, message.Attempts);
    }

    [Fact]
    public void MarkProcessed_SetsProcessedAtUtc()
    {
        var message = OutboxMessage.For("customer.upserted", "{}", Now);

        message.MarkProcessed(Now.AddSeconds(1));

        Assert.Equal(Now.AddSeconds(1), message.ProcessedAtUtc);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 8)]
    [InlineData(6, 60)]
    public void RecordFailure_BacksOffExponentiallyUpToTheCap(int failures, int expectedDelaySeconds)
    {
        var message = OutboxMessage.For("customer.upserted", "{}", Now);

        for (var i = 0; i < failures; i++)
        {
            message.RecordFailure("boom", Now, maxAttempts: 10, MaxBackoff);
        }

        Assert.Equal(failures, message.Attempts);
        Assert.Equal("boom", message.LastError);
        Assert.Equal(Now.AddSeconds(expectedDelaySeconds), message.NextAttemptAtUtc);
        Assert.Null(message.DeadLetteredAtUtc);
    }

    [Fact]
    public void RecordFailure_DeadLettersOnceMaxAttemptsIsReached()
    {
        var message = OutboxMessage.For("customer.upserted", "{}", Now);

        for (var i = 0; i < 4; i++)
        {
            message.RecordFailure("boom", Now, maxAttempts: 5, MaxBackoff);
        }

        Assert.Null(message.DeadLetteredAtUtc);

        message.RecordFailure("boom", Now, maxAttempts: 5, MaxBackoff);

        Assert.Equal(5, message.Attempts);
        Assert.Equal(Now, message.DeadLetteredAtUtc);
    }
}
