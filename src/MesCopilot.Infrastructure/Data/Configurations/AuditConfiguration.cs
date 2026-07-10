using MesCopilot.Domain.Entities.Auditing;
using MesCopilot.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesCopilot.Infrastructure.Data.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(item => item.Method).HasMaxLength(16).IsRequired();
        builder.Property(item => item.RouteTemplate).HasMaxLength(512).IsRequired();
        builder.Property(item => item.QueryKeys).HasMaxLength(2048).IsRequired();
        builder.Property(item => item.UserId).HasMaxLength(450);
        builder.Property(item => item.CorrelationId).HasMaxLength(128);
        builder.Property(item => item.IpHash).HasMaxLength(64);
        builder.HasIndex(item => new { item.TenantId, item.Timestamp });
        builder.HasIndex(item => new { item.TenantId, item.UserId, item.Timestamp });
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DataChangeLogConfiguration : IEntityTypeConfiguration<DataChangeLog>
{
    public void Configure(EntityTypeBuilder<DataChangeLog> builder)
    {
        builder.ToTable("DataChangeLogs");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(item => item.UserId).HasMaxLength(450);
        builder.Property(item => item.EntityType).HasMaxLength(256).IsRequired();
        builder.Property(item => item.EntityId).HasMaxLength(256).IsRequired();
        builder.Property(item => item.ChangeType).HasMaxLength(16).IsRequired();
        builder.Property(item => item.NewValues).IsRequired();
        builder.Property(item => item.CorrelationId).HasMaxLength(128);
        builder.HasIndex(item => new { item.TenantId, item.Timestamp });
        builder.HasIndex(item => new { item.TenantId, item.EntityType, item.EntityId });
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AgentAuditLogConfiguration : IEntityTypeConfiguration<AgentAuditLog>
{
    public void Configure(EntityTypeBuilder<AgentAuditLog> builder)
    {
        builder.ToTable("AgentAuditLogs");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(item => item.UserId).HasMaxLength(450).IsRequired();
        builder.Property(item => item.Status).HasMaxLength(32).IsRequired();
        builder.Property(item => item.ToolName).HasMaxLength(256);
        builder.Property(item => item.QueryDigest).HasMaxLength(64);
        builder.Property(item => item.CorrelationId).HasMaxLength(128);
        builder.HasIndex(item => new { item.TenantId, item.Timestamp });
        builder.HasIndex(item => new { item.TenantId, item.UserId, item.Timestamp });
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
