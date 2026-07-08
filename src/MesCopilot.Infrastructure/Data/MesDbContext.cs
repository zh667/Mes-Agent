using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Entities.Knowledge;
using MesCopilot.Domain.Entities.Products;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Entities.Quality;
using Microsoft.EntityFrameworkCore;
using EquipmentEntity = MesCopilot.Domain.Entities.Equipment.Equipment;

namespace MesCopilot.Infrastructure.Data;

public class MesDbContext : DbContext
{
    public MesDbContext(DbContextOptions<MesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Material> Materials => Set<Material>();

    public DbSet<Bom> Boms => Set<Bom>();

    public DbSet<BomItem> BomItems => Set<BomItem>();

    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();

    public DbSet<ProcessRoute> ProcessRoutes => Set<ProcessRoute>();

    public DbSet<ProcessStep> ProcessSteps => Set<ProcessStep>();

    public DbSet<Workstation> Workstations => Set<Workstation>();

    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    public DbSet<WorkOrderOperation> WorkOrderOperations => Set<WorkOrderOperation>();

    public DbSet<ProductionReport> ProductionReports => Set<ProductionReport>();

    public DbSet<QualityInspection> QualityInspections => Set<QualityInspection>();

    public DbSet<DefectRecord> DefectRecords => Set<DefectRecord>();

    public DbSet<DefectType> DefectTypes => Set<DefectType>();

    public DbSet<EquipmentEntity> Equipment => Set<EquipmentEntity>();

    public DbSet<EquipmentStatus> EquipmentStatuses => Set<EquipmentStatus>();

    public DbSet<EquipmentAlarm> EquipmentAlarms => Set<EquipmentAlarm>();

    public DbSet<DowntimeRecord> DowntimeRecords => Set<DowntimeRecord>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MesDbContext).Assembly);
    }
}
