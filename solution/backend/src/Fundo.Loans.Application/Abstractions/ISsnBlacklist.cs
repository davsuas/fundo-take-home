using Fundo.Loans.Domain.ValueObjects;

namespace Fundo.Loans.Application.Abstractions;

/// <summary>Port for checking whether an SSN's hash is on the blacklist. Implemented in Infrastructure against the `blacklisted_ssns` table.</summary>
public interface ISsnBlacklist
{
    Task<bool> ContainsAsync(Ssn ssn, CancellationToken cancellationToken);
}
