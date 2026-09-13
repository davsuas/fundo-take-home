using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Fundo.Loans.Infrastructure.Persistence;

/// <summary>
/// Applies committed EF Core migrations and idempotently seeds the blacklist. Invoked only by
/// the one-shot `migrate` compose service (`dotnet Fundo.Loans.Api.dll migrate`) — the running
/// api/worker processes never migrate the schema themselves (immutable infrastructure).
/// </summary>
public static class Migrator
{
    /// <summary>The blacklisted SSNs documented in README.md "Test data".</summary>
    private static readonly (string Ssn, string Label)[] SeedBlacklist =
    [
        ("111-11-1111", "seed-blacklist-1"),
        ("222-22-2222", "seed-blacklist-2"),
        ("333-33-3333", "seed-blacklist-3"),
    ];

    public static async Task MigrateAndSeedAsync(LoansDbContext dbContext, ISsnHasher hasher, CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        foreach (var (raw, label) in SeedBlacklist)
        {
            var hash = hasher.Hash(Ssn.Create(raw));

            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO blacklisted_ssns (ssn_hash, label) VALUES ({hash}, {label}) ON CONFLICT (ssn_hash) DO NOTHING",
                cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
