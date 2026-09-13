namespace Fundo.Loans.Application.Abstractions;

/// <summary>
/// Thrown by <see cref="IUnitOfWork"/> when another transaction committed a conflicting row first
/// (e.g. two first-time submissions with the same SSN racing on the unique index). The transaction
/// has already been rolled back, so the caller can safely retry the unit of work.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
