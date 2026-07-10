using MesCopilot.Domain.Common;
using MesCopilot.Domain.Entities.Conversations;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Entities.Knowledge;
using MesCopilot.Domain.Entities.Products;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Entities.Quality;
using MesCopilot.Infrastructure.Data;
using MesCopilot.UnitTests.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using EquipmentEntity = MesCopilot.Domain.Entities.Equipment.Equipment;

namespace MesCopilot.UnitTests.Infrastructure.Data;

public class MesDbContextTests
{
    [Fact]
    public void MesDbContext_ShouldExposeAllDomainDbSets()
    {
        using var context = CreateContext();

        Assert.NotNull(context.Products);
        Assert.NotNull(context.Materials);
        Assert.NotNull(context.Boms);
        Assert.NotNull(context.BomItems);
        Assert.NotNull(context.ProductionLines);
        Assert.NotNull(context.ProcessRoutes);
        Assert.NotNull(context.ProcessSteps);
        Assert.NotNull(context.Workstations);
        Assert.NotNull(context.WorkOrders);
        Assert.NotNull(context.WorkOrderOperations);
        Assert.NotNull(context.ProductionReports);
        Assert.NotNull(context.QualityInspections);
        Assert.NotNull(context.DefectRecords);
        Assert.NotNull(context.DefectTypes);
        Assert.NotNull(context.Equipment);
        Assert.NotNull(context.EquipmentStatuses);
        Assert.NotNull(context.EquipmentAlarms);
        Assert.NotNull(context.DowntimeRecords);
        Assert.NotNull(context.Documents);
        Assert.NotNull(context.DocumentChunks);
    }

    [Fact]
    public void MesDbContext_ShouldIgnoreComputedWorkOrderProgress()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(WorkOrder));

        Assert.NotNull(entity);
        Assert.Null(entity.FindProperty(nameof(WorkOrder.Progress)));
    }

    [Fact]
    public void MesDbContext_ShouldTenantScopeEveryBusinessEntity()
    {
        using MesDbContext context = CreateContext();
        var businessEntityTypes = context.Model.GetEntityTypes()
            .Where(entityType => entityType.ClrType.Namespace?.StartsWith(
                "MesCopilot.Domain.Entities",
                StringComparison.Ordinal) == true)
            .Where(entityType => entityType.ClrType.Namespace != "MesCopilot.Domain.Entities.Identity")
            .ToList();

        Assert.NotEmpty(businessEntityTypes);
        foreach (var entityType in businessEntityTypes)
        {
            Assert.True(
                typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType),
                $"{entityType.ClrType.Name} must implement {nameof(ITenantEntity)}.");
            Assert.NotNull(entityType.GetQueryFilter());
            Assert.False(entityType.FindProperty(nameof(ITenantEntity.TenantId))?.IsNullable);
            Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
                foreignKey.PrincipalEntityType.ClrType == typeof(MesCopilot.Domain.Entities.Identity.Tenant));
        }
    }

    [Theory]
    [InlineData(typeof(Product), "Code")]
    [InlineData(typeof(Material), "Code")]
    [InlineData(typeof(Bom), "Code")]
    [InlineData(typeof(WorkOrder), "Code")]
    [InlineData(typeof(ProductionLine), "Code")]
    [InlineData(typeof(EquipmentEntity), "Code")]
    public void MesDbContext_ShouldConfigureUniqueCodeIndexes(Type entityType, string propertyName)
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(entityType);
        var property = entity?.FindProperty(propertyName);
        var index = entity?.GetIndexes().SingleOrDefault(index =>
            index.Properties.Select(item => item.Name)
                .SequenceEqual(["TenantId", propertyName]));

        Assert.NotNull(entity);
        Assert.NotNull(property);
        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }

    [Theory]
    [InlineData(typeof(WorkOrder), nameof(WorkOrder.CreatedAt))]
    [InlineData(typeof(WorkOrder), nameof(WorkOrder.PlannedEndTime))]
    [InlineData(typeof(QualityInspection), nameof(QualityInspection.InspectionTime))]
    public void MesDbContext_ShouldConfigureReadQueryIndexes(Type entityType, string propertyName)
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(entityType);
        var property = entity?.FindProperty(propertyName);
        var index = entity?.GetIndexes().SingleOrDefault(index =>
            index.Properties.Select(item => item.Name)
                .SequenceEqual(["TenantId", propertyName]));

        Assert.NotNull(entity);
        Assert.NotNull(property);
        Assert.NotNull(index);
    }

    [Fact]
    public void MesDbContext_ShouldRequireUniqueProcessStepSequencePerRoute()
    {
        using MesDbContext context = CreateContext();
        var index = context.Model.FindEntityType(typeof(ProcessStep))?.GetIndexes()
            .SingleOrDefault(candidate => candidate.Properties.Select(property => property.Name)
                .SequenceEqual(["TenantId", "ProcessRouteId", "Sequence"]));

        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }

    [Fact]
    public void MesDbContext_ShouldKeepVerificationPayloadAndSchemaVersionConsistent()
    {
        using MesDbContext context = CreateContext();
        IModel model = context.GetService<IDesignTimeModel>().Model;
        var constraint = model.FindEntityType(typeof(ConversationMessage))?
            .GetCheckConstraints()
            .SingleOrDefault(item => item.Name == "CK_ConversationMessages_VerificationState");

        Assert.NotNull(constraint);
        Assert.Contains("VerificationJson", constraint.Sql, StringComparison.Ordinal);
        Assert.Contains("VerificationSchemaVersion", constraint.Sql, StringComparison.Ordinal);
    }

    private static MesDbContext CreateContext()
    {
        return TenantTestDbContextFactory.Create();
    }
}
