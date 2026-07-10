using MesCopilot.Domain.Entities.Equipment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentEntity = MesCopilot.Domain.Entities.Equipment.Equipment;

namespace MesCopilot.Infrastructure.Data.Configurations;

public class EquipmentConfiguration : IEntityTypeConfiguration<EquipmentEntity>
{
    public void Configure(EntityTypeBuilder<EquipmentEntity> builder)
    {
        builder.ToTable("Equipment");
        builder.HasKey(equipment => equipment.Id);
        builder.ConfigureTenantEntity();

        builder.Property(equipment => equipment.Code).IsRequired().HasMaxLength(50);
        builder.Property(equipment => equipment.Name).IsRequired().HasMaxLength(200);
        builder.Property(equipment => equipment.Model).HasMaxLength(100);
        builder.Property(equipment => equipment.IdealCycleTime).HasPrecision(10, 2);

        builder.HasIndex(equipment => new { equipment.TenantId, equipment.Code }).IsUnique();

        builder.HasOne(equipment => equipment.ProductionLine)
            .WithMany()
            .HasForeignKey(equipment => new { equipment.TenantId, equipment.ProductionLineId })
            .HasPrincipalKey(line => new { line.TenantId, line.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(equipment => equipment.Workstation)
            .WithMany()
            .HasForeignKey(equipment => new { equipment.TenantId, equipment.WorkstationId })
            .HasPrincipalKey(workstation => new { workstation.TenantId, workstation.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EquipmentStatusConfiguration : IEntityTypeConfiguration<EquipmentStatus>
{
    public void Configure(EntityTypeBuilder<EquipmentStatus> builder)
    {
        builder.ToTable("EquipmentStatuses");
        builder.HasKey(status => status.Id);
        builder.ConfigureTenantEntity();

        builder.Property(status => status.State).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(status => new { status.TenantId, status.EquipmentId, status.StartTime });

        builder.HasOne(status => status.Equipment)
            .WithMany(equipment => equipment.StatusHistory)
            .HasForeignKey(status => new { status.TenantId, status.EquipmentId })
            .HasPrincipalKey(equipment => new { equipment.TenantId, equipment.Id });
    }
}

public class EquipmentAlarmConfiguration : IEntityTypeConfiguration<EquipmentAlarm>
{
    public void Configure(EntityTypeBuilder<EquipmentAlarm> builder)
    {
        builder.ToTable("EquipmentAlarms");
        builder.HasKey(alarm => alarm.Id);
        builder.ConfigureTenantEntity();

        builder.Property(alarm => alarm.AlarmCode).IsRequired().HasMaxLength(50);
        builder.Property(alarm => alarm.Message).IsRequired().HasMaxLength(500);
        builder.HasIndex(alarm => new { alarm.TenantId, alarm.EquipmentId, alarm.OccurredAt });

        builder.HasOne(alarm => alarm.Equipment)
            .WithMany(equipment => equipment.Alarms)
            .HasForeignKey(alarm => new { alarm.TenantId, alarm.EquipmentId })
            .HasPrincipalKey(equipment => new { equipment.TenantId, equipment.Id });
    }
}

public class DowntimeRecordConfiguration : IEntityTypeConfiguration<DowntimeRecord>
{
    public void Configure(EntityTypeBuilder<DowntimeRecord> builder)
    {
        builder.ToTable("DowntimeRecords");
        builder.HasKey(record => record.Id);
        builder.ConfigureTenantEntity();

        builder.Property(record => record.Reason).IsRequired().HasMaxLength(200);
        builder.Property(record => record.DowntimeType).IsRequired().HasMaxLength(50);

        builder.HasOne(record => record.Equipment)
            .WithMany(equipment => equipment.DowntimeRecords)
            .HasForeignKey(record => new { record.TenantId, record.EquipmentId })
            .HasPrincipalKey(equipment => new { equipment.TenantId, equipment.Id });
    }
}
