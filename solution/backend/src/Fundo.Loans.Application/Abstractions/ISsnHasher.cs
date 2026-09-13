using Fundo.Loans.Domain.ValueObjects;

namespace Fundo.Loans.Application.Abstractions;

/// <summary>Deterministic, one-way hash of an SSN's 9 digits, used for lookup and blacklist comparison. Plaintext SSN is never persisted.</summary>
public interface ISsnHasher
{
    string Hash(Ssn ssn);
}
