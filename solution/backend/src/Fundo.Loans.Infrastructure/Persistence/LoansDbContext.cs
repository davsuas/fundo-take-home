using Fundo.Loans.Domain.Entities;
using Fundo.Loans.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Fundo.Loans.Infrastructure.Persistence;

public sealed class LoansDbContext : DbContext
{
    public LoansDbContext(DbContextOptions<LoansDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();
    public DbSet<BlacklistedSsn> BlacklistedSsns => Set<BlacklistedSsn>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new LoanApplicationConfiguration());
        modelBuilder.ApplyConfiguration(new BlacklistedSsnConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
