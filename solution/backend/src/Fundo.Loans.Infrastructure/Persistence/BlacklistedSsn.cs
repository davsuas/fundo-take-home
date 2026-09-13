namespace Fundo.Loans.Infrastructure.Persistence;

/// <summary>
/// A seeded blacklist row. Not a domain entity — it carries no behaviour, only the hashed SSN
/// and a human label, and exists purely as storage backing <see cref="Fundo.Loans.Application.Abstractions.ISsnBlacklist"/>.
/// </summary>
public sealed class BlacklistedSsn
{
    public required string SsnHash { get; init; }

    public required string Label { get; init; }
}
