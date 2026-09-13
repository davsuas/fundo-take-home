using Fundo.Loans.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Fundo.Loans.Infrastructure.Persistence;

/// <summary>
/// One business transaction: begin -&gt; run the delegate (which stages entity changes, including
/// the outbox row) -&gt; save -&gt; commit. If anything throws before the commit, disposing the
/// transaction rolls it back, so there is no half-saved customer, no orphan application and no
/// outbox row. The change tracker is cleared so the same scope can retry cleanly.
/// </summary>
public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly LoansDbContext _dbContext;

    public EfUnitOfWork(LoansDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var result = await operation(cancellationToken).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            _dbContext.ChangeTracker.Clear();
            throw new ConcurrencyConflictException("A concurrent transaction committed a conflicting row.", ex);
        }
        catch
        {
            _dbContext.ChangeTracker.Clear();
            throw;
        }
    }
}
