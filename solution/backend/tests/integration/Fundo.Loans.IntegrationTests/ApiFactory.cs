using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Fundo.Loans.IntegrationTests;

/// <summary>Boots the real API (Program.cs) against a real postgres:18-alpine container, migrated and seeded exactly like the compose `migrate` step.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        await Migrator.MigrateAndSeedAsync(
            scope.ServiceProvider.GetRequiredService<LoansDbContext>(),
            scope.ServiceProvider.GetRequiredService<ISsnHasher>(),
            TestContext.Current.CancellationToken);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Loans"] = _postgres.GetConnectionString(),
            ["SsnHashing:Pepper"] = "integration-test-pepper",
        }));
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
