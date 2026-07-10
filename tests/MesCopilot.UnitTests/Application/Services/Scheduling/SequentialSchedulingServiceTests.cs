using MesCopilot.Application.Services.Scheduling;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Entities.Products;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Data.Interceptors;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using EquipmentEntity = MesCopilot.Domain.Entities.Equipment.Equipment;

namespace MesCopilot.UnitTests.Application.Services.Scheduling;

public class SequentialSchedulingServiceTests
{
    [Fact]
    public async Task GenerateAsync_SchedulesOperationsInRouteOrderWithoutOverlap()
    {
        await using MesDbContext context = CreateContext();
        (WorkOrder order, EquipmentEntity equipment) = await SeedAsync(context);
        DateTime start = new(2026, 7, 10, 8, 0, 0, DateTimeKind.Utc);

        var result = await new SequentialSchedulingService(context).GenerateAsync([order.Id], start);

        Assert.Equal(2, result.Operations.Count);
        Assert.All(result.Operations, operation => Assert.Equal(equipment.Id, operation.EquipmentId));
        Assert.True(result.Operations[0].PlannedEndTime <= result.Operations[1].PlannedStartTime);
        Assert.Equal(WorkOrderStatus.Scheduled, context.WorkOrders.Single().Status);
    }

    [Fact]
    public async Task GenerateAsync_RejectsMoreThanFiftyWorkOrders()
    {
        await using MesDbContext context = CreateContext();
        int[] ids = Enumerable.Range(1, 51).ToArray();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => new SequentialSchedulingService(context).GenerateAsync(ids, DateTime.UtcNow));
    }

    [Fact]
    public async Task AdjustOperationAsync_RejectsEquipmentOutsideActiveTenant()
    {
        await using MesDbContext context = CreateContext();
        (WorkOrder order, _) = await SeedAsync(context);
        await new SequentialSchedulingService(context).GenerateAsync([order.Id], DateTime.UtcNow);
        WorkOrderOperation operation = await context.WorkOrderOperations.FirstAsync();

        await Assert.ThrowsAsync<ScheduleValidationException>(() =>
            new SequentialSchedulingService(context).AdjustOperationAsync(
                operation.Id,
                equipmentId: int.MaxValue,
                operation.PlannedStartTime,
                operation.PlannedEndTime,
                operation.Version));
    }

    [Fact]
    public async Task AdjustOperationAsync_RejectsCompletedWorkOrder()
    {
        await using MesDbContext context = CreateContext();
        (WorkOrder order, EquipmentEntity equipment) = await SeedAsync(context);
        await new SequentialSchedulingService(context).GenerateAsync([order.Id], DateTime.UtcNow);
        WorkOrderOperation operation = await context.WorkOrderOperations.FirstAsync();
        order.Status = WorkOrderStatus.Completed;
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ScheduleValidationException>(() =>
            new SequentialSchedulingService(context).AdjustOperationAsync(
                operation.Id,
                equipment.Id,
                operation.PlannedStartTime,
                operation.PlannedEndTime,
                operation.Version));
    }

    [Fact]
    public async Task GetGanttAsync_RejectsRangeAboveThirtyOneDays()
    {
        await using MesDbContext context = CreateContext();
        DateTime from = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            new SequentialSchedulingService(context).GetGanttAsync(from, from.AddDays(32)));
    }

    private static async Task<(WorkOrder, EquipmentEntity)> SeedAsync(MesDbContext context)
    {
        ProductionLine line = new() { Code = "L1", Name = "Line 1" };
        Workstation station = new() { Code = "WS1", Name = "Station 1", ProductionLine = line };
        Product product = new() { Code = "P1", Name = "Product", Unit = "pcs" };
        ProcessRoute route = new() { Code = "R1", Name = "Route", Product = product, IsActive = true };
        route.ProcessSteps.Add(new ProcessStep { Code = "S1", Name = "Cut", Workstation = station, Sequence = 1, StandardTime = 2m });
        route.ProcessSteps.Add(new ProcessStep { Code = "S2", Name = "Inspect", Workstation = station, Sequence = 2, StandardTime = 1m });
        EquipmentEntity equipment = new() { Code = "EQ1", Name = "Machine", ProductionLine = line, Workstation = station, IsActive = true };
        WorkOrder order = new()
        {
            Code = "WO1",
            Product = product,
            ProductionLine = line,
            PlannedQuantity = 10,
            PlannedStartTime = DateTime.UtcNow,
            PlannedEndTime = DateTime.UtcNow.AddDays(1),
            Status = WorkOrderStatus.NotScheduled
        };
        context.AddRange(route, equipment, order);
        await context.SaveChangesAsync();
        return (order, equipment);
    }

    private static MesDbContext CreateContext()
    {
        CurrentTenantContext tenant = new();
        tenant.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: false));
        DbContextOptions<MesDbContext> options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase($"schedule-{Guid.NewGuid():N}")
            .AddInterceptors(new TenantWriteGuardInterceptor(tenant))
            .Options;
        return new MesDbContext(options, tenant);
    }
}
