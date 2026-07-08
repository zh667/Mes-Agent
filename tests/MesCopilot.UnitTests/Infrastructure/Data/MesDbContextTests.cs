using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Entities.Knowledge;
using MesCopilot.Domain.Entities.Products;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Entities.Quality;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
        var index = property is null ? null : entity?.FindIndex(property);

        Assert.NotNull(entity);
        Assert.NotNull(property);
        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }

    private static MesDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MesDbContext(options);
    }
}
