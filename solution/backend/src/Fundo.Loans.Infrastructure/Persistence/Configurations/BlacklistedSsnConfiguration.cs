using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fundo.Loans.Infrastructure.Persistence.Configurations;

public sealed class BlacklistedSsnConfiguration : IEntityTypeConfiguration<BlacklistedSsn>
{
    public void Configure(EntityTypeBuilder<BlacklistedSsn> builder)
    {
        builder.ToTable("blacklisted_ssns");

        builder.HasKey(b => b.SsnHash);
        builder.Property(b => b.SsnHash).HasColumnName("ssn_hash").HasMaxLength(64);
        builder.Property(b => b.Label).HasColumnName("label").HasMaxLength(100).IsRequired();
    }
}
