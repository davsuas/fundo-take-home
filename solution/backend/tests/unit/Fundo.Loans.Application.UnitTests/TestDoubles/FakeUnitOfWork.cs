using Fundo.Loans.Application.Abstractions;

namespace Fundo.Loans.Application.UnitTests.TestDoubles;

/// <summary>Runs the operation inline — stands in for "the transaction commits".</summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Executions { get; private set; }

    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        Executions++;
        return operation(cancellationToken);
    }
}

/// <summary>Runs the operation, then fails the commit — stands in for "SaveChanges/commit fails and everything rolls back".</summary>
public sealed class FailingCommitUnitOfWork : IUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        await operation(cancellationToken);
        throw new InvalidOperationException("Simulated commit failure.");
    }
}

/// <summary>The first commit loses a unique-key race (another request inserted the same customer); later commits succeed.</summary>
public sealed class ConflictOnFirstCommitUnitOfWork : IUnitOfWork
{
    public int Executions { get; private set; }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        Executions++;
        var result = await operation(cancellationToken);

        if (Executions == 1)
        {
            throw new ConcurrencyConflictException("Simulated unique violation.", new InvalidOperationException());
        }

        return result;
    }
}

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
