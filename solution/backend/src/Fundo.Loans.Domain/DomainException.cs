namespace Fundo.Loans.Domain;

/// <summary>
/// Raised when a domain invariant is violated (invalid value object input, illegal state
/// transition). The only exception type the domain layer throws — never used for control flow
/// in the happy path.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
