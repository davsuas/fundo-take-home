namespace Fundo.Loans.Application.Abstractions;

/// <summary>
/// Wraps a single database transaction around a unit of work: begin, run the delegate, save
/// changes, commit. Any exception rolls everything back — no half-saved customer, no orphan
/// application, no outbox row. A lost race on a unique key surfaces as
/// <see cref="ConcurrencyConflictException"/>.
/// </summary>
public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
