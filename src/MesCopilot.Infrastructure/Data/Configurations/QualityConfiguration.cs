using MesCopilot.Domain.Entities.Quality;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesCopilot.Infrastructure.Data.Configurations;

public class QualityInspectionConfiguration : IEntityTypeConfiguration<QualityInspection>
{
    public void Configure(EntityTypeBuilder<QualityInspection> builder)
    {
        builder.ToTable("QualityInspections");
        builder.HasKey(inspection => inspection.Id);
        builder.ConfigureTenantEntity();

        builder.Property(inspection => inspection.Code).IsRequired().HasMaxLength(50);
        builder.Property(inspection => inspection.BatchNumber).IsRequired().HasMaxLength(50);
        builder.Property(inspection => inspection.InspectorId).IsRequired().HasMaxLength(50);
        builder.Property(inspection => inspection.InspectorName).IsRequired().HasMaxLength(100);
        builder.Property(inspection => inspection.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(inspection => new { inspection.TenantId, inspection.Code }).IsUnique();
        builder.HasIndex(inspection => new { inspection.TenantId, inspection.BatchNumber });
        builder.HasIndex(inspection => new { inspection.TenantId, inspection.InspectionTime });

        builder.HasOne(inspection => inspection.WorkOrder)
            .WithMany()
            .HasForeignKey(inspection => new { inspection.TenantId, inspection.WorkOrderId })
            .HasPrincipalKey(workOrder => new { workOrder.TenantId, workOrder.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(inspection => inspection.ProcessStep)
            .WithMany()
            .HasForeignKey(inspection => new { inspection.TenantId, inspection.ProcessStepId })
            .HasPrincipalKey(step => new { step.TenantId, step.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DefectTypeConfiguration : IEntityTypeConfiguration<DefectType>
{
    public void Configure(EntityTypeBuilder<DefectType> builder)
    {
        builder.ToTable("DefectTypes");
        builder.HasKey(type => type.Id);
        builder.ConfigureTenantEntity();

        builder.Property(type => type.Code).IsRequired().HasMaxLength(50);
        builder.Property(type => type.Name).IsRequired().HasMaxLength(200);

        builder.HasIndex(type => new { type.TenantId, type.Code }).IsUnique();
    }
}

public class DefectRecordConfiguration : IEntityTypeConfiguration<DefectRecord>
{
    public void Configure(EntityTypeBuilder<DefectRecord> builder)
    {
        builder.ToTable("DefectRecords");
        builder.HasKey(record => record.Id);
        builder.ConfigureTenantEntity();

        builder.Property(record => record.DisposalMethod).HasMaxLength(50);

        builder.HasOne(record => record.QualityInspection)
            .WithMany(inspection => inspection.DefectRecords)
            .HasForeignKey(record => new { record.TenantId, record.QualityInspectionId })
            .HasPrincipalKey(inspection => new { inspection.TenantId, inspection.Id });

        builder.HasOne(record => record.DefectType)
            .WithMany(type => type.DefectRecords)
            .HasForeignKey(record => new { record.TenantId, record.DefectTypeId })
            .HasPrincipalKey(type => new { type.TenantId, type.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
