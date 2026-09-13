using Fundo.Loans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Fundo.Loans.IntegrationTests;

/// <summary>A real, migrated postgres:18-alpine container. Each fixture instance owns its own container.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public LoansDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<LoansDbContext>().UseNpgsql(ConnectionString).Options);

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
