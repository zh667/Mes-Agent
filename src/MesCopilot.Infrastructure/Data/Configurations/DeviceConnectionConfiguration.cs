using MesCopilot.Domain.Entities.Equipment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesCopilot.Infrastructure.Data.Configurations;

public sealed class DeviceConnectionConfiguration : IEntityTypeConfiguration<DeviceConnection>
{
    public void Configure(EntityTypeBuilder<DeviceConnection> builder)
    {
        builder.ToTable("DeviceConnections");
        builder.HasKey(connection => connection.Id);
        builder.ConfigureTenantEntity();
        builder.Property(connection => connection.Name).HasMaxLength(200).IsRequired();
        builder.Property(connection => connection.Protocol).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(connection => connection.EncryptedConfiguration).IsRequired();
        builder.Property(connection => connection.Host).HasMaxLength(255).IsRequired();
        builder.Property(connection => connection.Endpoint).HasMaxLength(500).IsRequired();
        builder.Property(connection => connection.LastErrorCode).HasMaxLength(100);
        builder.HasIndex(connection => new { connection.TenantId, connection.EquipmentId, connection.Protocol });
        builder.HasOne(connection => connection.Equipment)
            .WithMany(equipment => equipment.DeviceConnections)
            .HasForeignKey(connection => new { connection.TenantId, connection.EquipmentId })
            .HasPrincipalKey(equipment => new { equipment.TenantId, equipment.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
