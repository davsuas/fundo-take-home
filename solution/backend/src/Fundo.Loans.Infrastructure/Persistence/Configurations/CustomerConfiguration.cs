using Fundo.Loans.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fundo.Loans.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.OwnsOne(c => c.Name, name =>
        {
            name.Property(n => n.First).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            name.Property(n => n.Last).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        });

        builder.OwnsOne(c => c.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("street").HasMaxLength(200).IsRequired();
            address.Property(a => a.City).HasColumnName("city").HasMaxLength(100).IsRequired();
            address.Property(a => a.State).HasColumnName("state").HasMaxLength(2).IsRequired();
            address.Property(a => a.PostalCode).HasColumnName("postal_code").HasMaxLength(5).IsRequired();
        });

        builder.Navigation(c => c.Name).IsRequired();
        builder.Navigation(c => c.Address).IsRequired();

        builder.Property(c => c.CompanyName).HasColumnName("company_name").HasMaxLength(200).IsRequired();
        builder.Property(c => c.SsnHash).HasColumnName("ssn_hash").HasMaxLength(64).IsRequired();
        builder.Property(c => c.SsnLast4).HasColumnName("ssn_last4").HasMaxLength(4).IsRequired();
        builder.Property(c => c.CreatedAtUtc).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.UpdatedAtUtc).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(c => c.SsnHash).IsUnique().HasDatabaseName("ix_customers_ssn_hash");
    }
}
