using MesCopilot.Domain.Entities.Production;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesCopilot.Infrastructure.Data.Configurations;

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("WorkOrders");
        builder.HasKey(workOrder => workOrder.Id);

        builder.Property(workOrder => workOrder.Code).IsRequired().HasMaxLength(50);
        builder.Property(workOrder => workOrder.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(workOrder => workOrder.Code).IsUnique();
        builder.HasIndex(workOrder => workOrder.Status);
        builder.HasIndex(workOrder => workOrder.PlannedStartTime);

        builder.HasOne(workOrder => workOrder.Product)
            .WithMany()
            .HasForeignKey(workOrder => workOrder.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(workOrder => workOrder.ProductionLine)
            .WithMany(line => line.WorkOrders)
            .HasForeignKey(workOrder => workOrder.ProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(workOrder => workOrder.Progress);
    }
}

public class ProductionLineConfiguration : IEntityTypeConfiguration<ProductionLine>
{
    public void Configure(EntityTypeBuilder<ProductionLine> builder)
    {
        builder.ToTable("ProductionLines");
        builder.HasKey(line => line.Id);

        builder.Property(line => line.Code).IsRequired().HasMaxLength(50);
        builder.Property(line => line.Name).IsRequired().HasMaxLength(200);

        builder.HasIndex(line => line.Code).IsUnique();
    }
}

public class ProcessRouteConfiguration : IEntityTypeConfiguration<ProcessRoute>
{
    public void Configure(EntityTypeBuilder<ProcessRoute> builder)
    {
        builder.ToTable("ProcessRoutes");
        builder.HasKey(route => route.Id);

        builder.Property(route => route.Code).IsRequired().HasMaxLength(50);
        builder.Property(route => route.Name).IsRequired().HasMaxLength(200);

        builder.HasIndex(route => route.Code).IsUnique();
    }
}

public class ProcessStepConfiguration : IEntityTypeConfiguration<ProcessStep>
{
    public void Configure(EntityTypeBuilder<ProcessStep> builder)
    {
        builder.ToTable("ProcessSteps");
        builder.HasKey(step => step.Id);

        builder.Property(step => step.Code).IsRequired().HasMaxLength(50);
        builder.Property(step => step.Name).IsRequired().HasMaxLength(200);
        builder.Property(step => step.StandardTime).HasPrecision(10, 2);
    }
}

public class WorkstationConfiguration : IEntityTypeConfiguration<Workstation>
{
    public void Configure(EntityTypeBuilder<Workstation> builder)
    {
        builder.ToTable("Workstations");
        builder.HasKey(workstation => workstation.Id);

        builder.Property(workstation => workstation.Code).IsRequired().HasMaxLength(50);
        builder.Property(workstation => workstation.Name).IsRequired().HasMaxLength(200);

        builder.HasIndex(workstation => workstation.Code).IsUnique();
    }
}

public class WorkOrderOperationConfiguration : IEntityTypeConfiguration<WorkOrderOperation>
{
    public void Configure(EntityTypeBuilder<WorkOrderOperation> builder)
    {
        builder.ToTable("WorkOrderOperations");
        builder.HasKey(operation => operation.Id);

        builder.HasOne(operation => operation.WorkOrder)
            .WithMany(workOrder => workOrder.Operations)
            .HasForeignKey(operation => operation.WorkOrderId);

        builder.HasOne(operation => operation.ProcessStep)
            .WithMany()
            .HasForeignKey(operation => operation.ProcessStepId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductionReportConfiguration : IEntityTypeConfiguration<ProductionReport>
{
    public void Configure(EntityTypeBuilder<ProductionReport> builder)
    {
        builder.ToTable("ProductionReports");
        builder.HasKey(report => report.Id);

        builder.Property(report => report.BatchNumber).IsRequired().HasMaxLength(50);
        builder.Property(report => report.OperatorId).IsRequired().HasMaxLength(50);
        builder.Property(report => report.OperatorName).IsRequired().HasMaxLength(100);

        builder.HasIndex(report => report.BatchNumber);
        builder.HasIndex(report => report.Timestamp);

        builder.HasOne(report => report.WorkOrder)
            .WithMany(workOrder => workOrder.ProductionReports)
            .HasForeignKey(report => report.WorkOrderId);

        builder.HasOne(report => report.ProcessStep)
            .WithMany()
            .HasForeignKey(report => report.ProcessStepId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
