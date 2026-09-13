using Fundo.Loans.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fundo.Loans.Infrastructure.Persistence.Configurations;

public sealed class LoanApplicationConfiguration : IEntityTypeConfiguration<LoanApplication>
{
    public void Configure(EntityTypeBuilder<LoanApplication> builder)
    {
        builder.ToTable("loan_applications");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.CustomerId).HasColumnName("customer_id").IsRequired();

        // No navigation property on Customer by design (it doesn't need one) — this still
        // creates a real FK constraint against customers(id), enforced at the database level.
        builder.HasOne<Customer>().WithMany().HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(a => a.RequestedAmount, amount =>
        {
            amount.Property(m => m.Amount).HasColumnName("requested_amount").HasColumnType("numeric(12,2)").IsRequired();
        });
        builder.Navigation(a => a.RequestedAmount).IsRequired();

        builder.Property(a => a.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.CreatedAtUtc).HasColumnName("created_at").IsRequired();
        builder.Property(a => a.UpdatedAtUtc).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(a => a.CustomerId).IsUnique().HasDatabaseName("ix_loan_applications_customer_id");
    }
}
