using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fundo.Loans.Infrastructure.Persistence;

/// <summary>Design-time factory so `dotnet ef migrations add` works without a running host. Never used at runtime — hosts configure the DbContext themselves via <see cref="DependencyInjection.AddLoansInfrastructure"/>.</summary>
public sealed class LoansDbContextFactory : IDesignTimeDbContextFactory<LoansDbContext>
{
    public LoansDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("MIGRATIONS_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=fundo_loans;Username=loans_migrator;Password=design-time-only";

        var optionsBuilder = new DbContextOptionsBuilder<LoansDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new LoansDbContext(optionsBuilder.Options);
    }
}
