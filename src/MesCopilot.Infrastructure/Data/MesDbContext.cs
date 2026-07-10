using System.Linq.Expressions;
using MesCopilot.Domain.Common;
using MesCopilot.Domain.Entities.Auditing;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Entities.Conversations;
using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Domain.Entities.Knowledge;
using MesCopilot.Domain.Entities.Products;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Entities.Quality;
using MesCopilot.Infrastructure.Auditing;
using MesCopilot.Infrastructure.Data.ReadRouting;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EquipmentEntity = MesCopilot.Domain.Entities.Equipment.Equipment;

namespace MesCopilot.Infrastructure.Data;

public class MesDbContext : IdentityDbContext<AppUser>
{
    private readonly ITenantContext _tenantContext;

    public MesDbContext(DbContextOptions<MesDbContext> options)
        : this(options, new CurrentTenantContext())
    {
    }

    public MesDbContext(
        DbContextOptions<MesDbContext> options,
        ITenantContext tenantContext,
        AuditRequestContext? auditRequestContext = null,
        IReadConsistencyContext? readConsistencyContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
        AuditRequestContext = auditRequestContext;
        ReadConsistencyContext = readConsistencyContext;
    }

    public string? CurrentTenantId => _tenantContext.TenantId;

    internal AuditRequestContext? AuditRequestContext { get; }

    internal IReadConsistencyContext? ReadConsistencyContext { get; }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<UserTenantMembership> UserTenantMemberships => Set<UserTenantMembership>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<DataChangeLog> DataChangeLogs => Set<DataChangeLog>();

    public DbSet<AgentAuditLog> AgentAuditLogs => Set<AgentAuditLog>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Material> Materials => Set<Material>();

    public DbSet<Bom> Boms => Set<Bom>();

    public DbSet<BomItem> BomItems => Set<BomItem>();

    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();

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

    public DbSet<DeviceConnection> DeviceConnections => Set<DeviceConnection>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MesDbContext).Assembly);
        ApplyTenantQueryFilters(modelBuilder);
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(entityType => typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType)))
        {
            ParameterExpression entityParameter = Expression.Parameter(entityType.ClrType, "entity");
            MemberExpression entityTenantId = Expression.Property(
                entityParameter,
                nameof(ITenantEntity.TenantId));
            MemberExpression currentTenantId = Expression.Property(
                Expression.Constant(this),
                nameof(CurrentTenantId));
            BinaryExpression tenantMatches = Expression.Equal(entityTenantId, currentTenantId);
            LambdaExpression filter = Expression.Lambda(tenantMatches, entityParameter);

            entityType.SetQueryFilter(filter);
        }
    }
}
