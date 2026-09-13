using Fundo.Loans.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fundo.Loans.Api.HealthChecks;

/// <summary>Backs `/health/ready` — the API is only "ready" once it can reach Postgres.</summary>
public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly LoansDbContext _dbContext;

    public PostgresHealthCheck(LoansDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);

        return canConnect
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Cannot connect to Postgres.");
    }
}
