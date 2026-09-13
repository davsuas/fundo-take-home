using Fundo.Loans.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fundo.Loans.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(m => m.Type).HasColumnName("type").HasMaxLength(100).IsRequired();
        builder.Property(m => m.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.OccurredAtUtc).HasColumnName("occurred_at").IsRequired();
        builder.Property(m => m.ProcessedAtUtc).HasColumnName("processed_at");
        builder.Property(m => m.Attempts).HasColumnName("attempts").IsRequired();
        builder.Property(m => m.NextAttemptAtUtc).HasColumnName("next_attempt_at").IsRequired();
        builder.Property(m => m.LastError).HasColumnName("last_error").HasMaxLength(2000);
        builder.Property(m => m.DeadLetteredAtUtc).HasColumnName("dead_lettered_at");

        builder.HasIndex(m => new { m.ProcessedAtUtc, m.NextAttemptAtUtc }).HasDatabaseName("ix_outbox_messages_processed_at_next_attempt_at");
    }
}
