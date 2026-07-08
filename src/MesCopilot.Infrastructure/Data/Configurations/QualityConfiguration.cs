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

        builder.Property(inspection => inspection.Code).IsRequired().HasMaxLength(50);
        builder.Property(inspection => inspection.BatchNumber).IsRequired().HasMaxLength(50);
        builder.Property(inspection => inspection.InspectorId).IsRequired().HasMaxLength(50);
        builder.Property(inspection => inspection.InspectorName).IsRequired().HasMaxLength(100);
        builder.Property(inspection => inspection.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(inspection => inspection.Code).IsUnique();
        builder.HasIndex(inspection => inspection.BatchNumber);

        builder.HasOne(inspection => inspection.WorkOrder)
            .WithMany()
            .HasForeignKey(inspection => inspection.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(inspection => inspection.ProcessStep)
            .WithMany()
            .HasForeignKey(inspection => inspection.ProcessStepId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DefectTypeConfiguration : IEntityTypeConfiguration<DefectType>
{
    public void Configure(EntityTypeBuilder<DefectType> builder)
    {
        builder.ToTable("DefectTypes");
        builder.HasKey(type => type.Id);

        builder.Property(type => type.Code).IsRequired().HasMaxLength(50);
        builder.Property(type => type.Name).IsRequired().HasMaxLength(200);

        builder.HasIndex(type => type.Code).IsUnique();
    }
}

public class DefectRecordConfiguration : IEntityTypeConfiguration<DefectRecord>
{
    public void Configure(EntityTypeBuilder<DefectRecord> builder)
    {
        builder.ToTable("DefectRecords");
        builder.HasKey(record => record.Id);

        builder.Property(record => record.DisposalMethod).HasMaxLength(50);

        builder.HasOne(record => record.QualityInspection)
            .WithMany(inspection => inspection.DefectRecords)
            .HasForeignKey(record => record.QualityInspectionId);

        builder.HasOne(record => record.DefectType)
            .WithMany(type => type.DefectRecords)
            .HasForeignKey(record => record.DefectTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
