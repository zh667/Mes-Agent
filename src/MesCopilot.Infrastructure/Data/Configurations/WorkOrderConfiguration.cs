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
        builder.ConfigureTenantEntity();

        builder.Property(workOrder => workOrder.Code).IsRequired().HasMaxLength(50);
        builder.Property(workOrder => workOrder.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(workOrder => new { workOrder.TenantId, workOrder.Code }).IsUnique();
        builder.HasIndex(workOrder => new { workOrder.TenantId, workOrder.Status });
        builder.HasIndex(workOrder => new { workOrder.TenantId, workOrder.PlannedStartTime });
        builder.HasIndex(workOrder => new { workOrder.TenantId, workOrder.PlannedEndTime });
        builder.HasIndex(workOrder => new { workOrder.TenantId, workOrder.CreatedAt });

        builder.HasOne(workOrder => workOrder.Product)
            .WithMany()
            .HasForeignKey(workOrder => new { workOrder.TenantId, workOrder.ProductId })
            .HasPrincipalKey(product => new { product.TenantId, product.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(workOrder => workOrder.ProductionLine)
            .WithMany(line => line.WorkOrders)
            .HasForeignKey(workOrder => new { workOrder.TenantId, workOrder.ProductionLineId })
            .HasPrincipalKey(line => new { line.TenantId, line.Id })
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
        builder.ConfigureTenantEntity();

        builder.Property(line => line.Code).IsRequired().HasMaxLength(50);
        builder.Property(line => line.Name).IsRequired().HasMaxLength(200);

        builder.HasIndex(line => new { line.TenantId, line.Code }).IsUnique();
    }
}

public class ProcessRouteConfiguration : IEntityTypeConfiguration<ProcessRoute>
{
    public void Configure(EntityTypeBuilder<ProcessRoute> builder)
    {
        builder.ToTable("ProcessRoutes");
        builder.HasKey(route => route.Id);
        builder.ConfigureTenantEntity();

        builder.Property(route => route.Code).IsRequired().HasMaxLength(50);
        builder.Property(route => route.Name).IsRequired().HasMaxLength(200);

        builder.HasIndex(route => new { route.TenantId, route.Code }).IsUnique();

        builder.HasOne(route => route.Product)
            .WithMany()
            .HasForeignKey(route => new { route.TenantId, route.ProductId })
            .HasPrincipalKey(product => new { product.TenantId, product.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProcessStepConfiguration : IEntityTypeConfiguration<ProcessStep>
{
    public void Configure(EntityTypeBuilder<ProcessStep> builder)
    {
        builder.ToTable("ProcessSteps");
        builder.HasKey(step => step.Id);
        builder.ConfigureTenantEntity();

        builder.Property(step => step.Code).IsRequired().HasMaxLength(50);
        builder.Property(step => step.Name).IsRequired().HasMaxLength(200);
        builder.Property(step => step.StandardTime).HasPrecision(10, 2);
        builder.HasIndex(step => new { step.TenantId, step.ProcessRouteId, step.Sequence }).IsUnique();

        builder.HasOne(step => step.ProcessRoute)
            .WithMany(route => route.ProcessSteps)
            .HasForeignKey(step => new { step.TenantId, step.ProcessRouteId })
            .HasPrincipalKey(route => new { route.TenantId, route.Id });

        builder.HasOne(step => step.Workstation)
            .WithMany(workstation => workstation.ProcessSteps)
            .HasForeignKey(step => new { step.TenantId, step.WorkstationId })
            .HasPrincipalKey(workstation => new { workstation.TenantId, workstation.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkstationConfiguration : IEntityTypeConfiguration<Workstation>
{
    public void Configure(EntityTypeBuilder<Workstation> builder)
    {
        builder.ToTable("Workstations");
        builder.HasKey(workstation => workstation.Id);
        builder.ConfigureTenantEntity();

        builder.Property(workstation => workstation.Code).IsRequired().HasMaxLength(50);
        builder.Property(workstation => workstation.Name).IsRequired().HasMaxLength(200);

        builder.HasIndex(workstation => new { workstation.TenantId, workstation.Code }).IsUnique();

        builder.HasOne(workstation => workstation.ProductionLine)
            .WithMany()
            .HasForeignKey(workstation => new { workstation.TenantId, workstation.ProductionLineId })
            .HasPrincipalKey(line => new { line.TenantId, line.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkOrderOperationConfiguration : IEntityTypeConfiguration<WorkOrderOperation>
{
    public void Configure(EntityTypeBuilder<WorkOrderOperation> builder)
    {
        builder.ToTable("WorkOrderOperations");
        builder.HasKey(operation => operation.Id);
        builder.ConfigureTenantEntity();
        builder.Property(operation => operation.Version).IsRowVersion();
        builder.HasIndex(operation => new
        {
            operation.TenantId,
            operation.EquipmentId,
            operation.PlannedStartTime,
            operation.PlannedEndTime
        });
        builder.HasIndex(operation => new { operation.TenantId, operation.WorkOrderId, operation.ProcessStepId }).IsUnique();

        builder.HasOne(operation => operation.WorkOrder)
            .WithMany(workOrder => workOrder.Operations)
            .HasForeignKey(operation => new { operation.TenantId, operation.WorkOrderId })
            .HasPrincipalKey(workOrder => new { workOrder.TenantId, workOrder.Id });

        builder.HasOne(operation => operation.ProcessStep)
            .WithMany()
            .HasForeignKey(operation => new { operation.TenantId, operation.ProcessStepId })
            .HasPrincipalKey(step => new { step.TenantId, step.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(operation => operation.Equipment)
            .WithMany()
            .HasForeignKey(operation => new { operation.TenantId, operation.EquipmentId })
            .HasPrincipalKey(equipment => new { equipment.TenantId, equipment.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductionReportConfiguration : IEntityTypeConfiguration<ProductionReport>
{
    public void Configure(EntityTypeBuilder<ProductionReport> builder)
    {
        builder.ToTable("ProductionReports");
        builder.HasKey(report => report.Id);
        builder.ConfigureTenantEntity();

        builder.Property(report => report.BatchNumber).IsRequired().HasMaxLength(50);
        builder.Property(report => report.OperatorId).IsRequired().HasMaxLength(50);
        builder.Property(report => report.OperatorName).IsRequired().HasMaxLength(100);

        builder.HasIndex(report => new { report.TenantId, report.BatchNumber });
        builder.HasIndex(report => new { report.TenantId, report.Timestamp });

        builder.HasOne(report => report.WorkOrder)
            .WithMany(workOrder => workOrder.ProductionReports)
            .HasForeignKey(report => new { report.TenantId, report.WorkOrderId })
            .HasPrincipalKey(workOrder => new { workOrder.TenantId, workOrder.Id });

        builder.HasOne(report => report.ProcessStep)
            .WithMany()
            .HasForeignKey(report => new { report.TenantId, report.ProcessStepId })
            .HasPrincipalKey(step => new { step.TenantId, step.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MesCopilot.Domain.Entities.Equipment.Equipment>()
            .WithMany()
            .HasForeignKey(report => new { report.TenantId, report.EquipmentId })
            .HasPrincipalKey(equipment => new { equipment.TenantId, equipment.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
