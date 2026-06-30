using FinanceFlow.Workers.Audit.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Workers.Audit.Persistence.Configuration;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Payload).HasColumnType("jsonb");
        builder.HasIndex(x => new { x.Topic, x.Partition, x.Offset }).IsUnique();
    }
}