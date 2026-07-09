using MesCopilot.Application.Services;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using EquipmentEntity = MesCopilot.Domain.Entities.Equipment.Equipment;

namespace MesCopilot.UnitTests.Application.Services;

public class EquipmentServiceTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnEquipment()
    {
        await using var context = CreateContext();
        context.Equipment.Add(new EquipmentEntity { Id = 1, Code = "A101", Name = "冲压机1号", RatedCapacity = 100, IdealCycleTime = 36, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
        var service = new EquipmentService(context);

        var result = (await service.GetAllAsync()).ToList();

        Assert.Single(result);
        Assert.Equal("A101", result[0].Code);
    }

    [Fact]
    public async Task GetStatusHistoryAsync_ShouldReturnStatusesForEquipment()
    {
        await using var context = CreateContext();
        context.Equipment.Add(new EquipmentEntity { Id = 1, Code = "A101", Name = "冲压机1号", CreatedAt = DateTime.UtcNow });
        context.EquipmentStatuses.AddRange(
            new EquipmentStatus { EquipmentId = 1, State = EquipmentState.Running, StartTime = DateTime.UtcNow.AddHours(-1), DurationMinutes = 60 },
            new EquipmentStatus { EquipmentId = 2, State = EquipmentState.Alarm, StartTime = DateTime.UtcNow.AddHours(-1), DurationMinutes = 60 });
        await context.SaveChangesAsync();
        var service = new EquipmentService(context);

        var result = (await service.GetStatusHistoryAsync(1)).ToList();

        Assert.Single(result);
        Assert.Equal(EquipmentState.Running, result[0].State);
    }

    [Fact]
    public async Task CalculateOeeAsync_ShouldReturnMetricsBetweenZeroAndOne()
    {
        await using var context = CreateContext();
        var today = DateTime.UtcNow.Date;
        context.Equipment.Add(new EquipmentEntity { Id = 1, Code = "A101", Name = "冲压机1号", RatedCapacity = 100, IdealCycleTime = 36, CreatedAt = today });
        context.EquipmentStatuses.Add(new EquipmentStatus { EquipmentId = 1, State = EquipmentState.Running, StartTime = today.AddHours(8), DurationMinutes = 60 });
        context.ProductionReports.Add(new ProductionReport
        {
            WorkOrderId = 1,
            ProcessStepId = 1,
            EquipmentId = 1,
            BatchNumber = "B20260708080000-1",
            OperatorId = "op-1",
            OperatorName = "张三",
            Timestamp = today.AddHours(9),
            Quantity = 100,
            QualifiedQuantity = 95,
            CreatedAt = today.AddHours(9)
        });
        await context.SaveChangesAsync();
        var service = new EquipmentService(context);

        var result = await service.CalculateOeeAsync(1, today);

        Assert.InRange(result.Availability, 0m, 1m);
        Assert.InRange(result.Performance, 0m, 1m);
        Assert.InRange(result.Quality, 0m, 1m);
        Assert.InRange(result.Oee, 0m, 1m);
    }

    private static MesDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MesDbContext(options);
    }
}
