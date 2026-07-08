using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Entities.Products;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.UnitTests.Application.Services;

public class WorkOrderServiceTests
{
    [Fact]
    public async Task GetTodayWorkOrdersAsync_ShouldReturnOnlyTodayOrders()
    {
        await using var context = CreateInMemoryContext();
        await SeedRequiredLookupsAsync(context);
        var today = DateTime.UtcNow.Date;

        context.WorkOrders.AddRange(
            new WorkOrder { Id = 1, Code = "WO-TODAY", ProductId = 1, ProductionLineId = 1, PlannedQuantity = 100, CreatedAt = today },
            new WorkOrder { Id = 2, Code = "WO-YESTERDAY", ProductId = 1, ProductionLineId = 1, PlannedQuantity = 100, CreatedAt = today.AddDays(-1) }
        );
        await context.SaveChangesAsync();
        var service = new WorkOrderService(context);

        var result = (await service.GetTodayWorkOrdersAsync()).ToList();

        Assert.Single(result);
        Assert.Equal("WO-TODAY", result[0].Code);
    }

    [Fact]
    public async Task StartAsync_ShouldUpdateStatusAndSetActualStartTime()
    {
        await using var context = CreateInMemoryContext();
        await SeedRequiredLookupsAsync(context);
        context.WorkOrders.Add(new WorkOrder
        {
            Id = 1,
            Code = "WO-001",
            ProductId = 1,
            ProductionLineId = 1,
            PlannedQuantity = 100,
            Status = WorkOrderStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var service = new WorkOrderService(context);

        var result = await service.StartAsync(1);

        Assert.Equal(WorkOrderStatus.InProgress, result.Status);
        Assert.NotNull(result.ActualStartTime);
    }

    [Fact]
    public async Task ReportAsync_ShouldIncreaseQuantitiesAndCreateProductionReport()
    {
        await using var context = CreateInMemoryContext();
        await SeedRequiredLookupsAsync(context);
        context.WorkOrders.Add(new WorkOrder
        {
            Id = 1,
            Code = "WO-001",
            ProductId = 1,
            ProductionLineId = 1,
            PlannedQuantity = 100,
            Status = WorkOrderStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var service = new WorkOrderService(context);

        var result = await service.ReportAsync(1, new ReportProductionRequest(10, 9, "op-1", "张三"));

        Assert.Equal(10, result.CompletedQuantity);
        Assert.Equal(9, result.QualifiedQuantity);
        Assert.Equal(1, await context.ProductionReports.CountAsync());
    }

    [Fact]
    public async Task CompleteAsync_ShouldSetCompletedStatusAndActualEndTime()
    {
        await using var context = CreateInMemoryContext();
        await SeedRequiredLookupsAsync(context);
        context.WorkOrders.Add(new WorkOrder
        {
            Id = 1,
            Code = "WO-001",
            ProductId = 1,
            ProductionLineId = 1,
            PlannedQuantity = 100,
            Status = WorkOrderStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var service = new WorkOrderService(context);

        var result = await service.CompleteAsync(1);

        Assert.Equal(WorkOrderStatus.Completed, result.Status);
        Assert.NotNull(result.ActualEndTime);
    }

    [Fact]
    public async Task GetDelayedWorkOrdersAsync_ShouldExcludeCompletedAndClosedOrders()
    {
        await using var context = CreateInMemoryContext();
        await SeedRequiredLookupsAsync(context);
        var now = DateTime.UtcNow;
        context.WorkOrders.AddRange(
            new WorkOrder { Id = 1, Code = "WO-DELAYED", ProductId = 1, ProductionLineId = 1, PlannedQuantity = 100, Status = WorkOrderStatus.InProgress, PlannedStartTime = now.AddDays(-3), PlannedEndTime = now.AddDays(-1), CreatedAt = now },
            new WorkOrder { Id = 2, Code = "WO-DONE", ProductId = 1, ProductionLineId = 1, PlannedQuantity = 100, Status = WorkOrderStatus.Completed, PlannedStartTime = now.AddDays(-3), PlannedEndTime = now.AddDays(-1), CreatedAt = now }
        );
        await context.SaveChangesAsync();
        var service = new WorkOrderService(context);

        var result = (await service.GetDelayedWorkOrdersAsync()).ToList();

        Assert.Single(result);
        Assert.Equal("WO-DELAYED", result[0].Code);
    }

    private static MesDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MesDbContext(options);
    }

    private static async Task SeedRequiredLookupsAsync(MesDbContext context)
    {
        context.Products.Add(new Product { Id = 1, Code = "PROD-A", Name = "产品A", Unit = "个", CreatedAt = DateTime.UtcNow });
        context.ProductionLines.Add(new ProductionLine { Id = 1, Code = "LINE-1", Name = "一号线", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
    }
}
