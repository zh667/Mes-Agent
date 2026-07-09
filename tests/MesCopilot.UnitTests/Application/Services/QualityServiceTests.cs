using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Entities.Quality;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.UnitTests.Application.Services;

public class QualityServiceTests
{
    [Fact]
    public async Task CreateInspectionAsync_ShouldPersistInspection()
    {
        await using var context = CreateContext();
        var service = new QualityService(context);

        var result = await service.CreateInspectionAsync(new CreateQualityInspectionRequest(
            "QI-001", "B20260708", 1, 1, "qc-1", "李四", 100, 98, 2, InspectionStatus.Fail, "尺寸异常"));

        Assert.Equal("QI-001", result.Code);
        Assert.Equal(1, await context.QualityInspections.CountAsync());
    }

    [Fact]
    public async Task TraceBatchAsync_ShouldReturnProductionReportsAndInspections()
    {
        await using var context = CreateContext();
        context.ProductionReports.Add(new ProductionReport { Id = 1, BatchNumber = "B1", WorkOrderId = 1, ProcessStepId = 1, EquipmentId = 1, OperatorId = "op", OperatorName = "张三", Quantity = 10, QualifiedQuantity = 9, Timestamp = DateTime.UtcNow, CreatedAt = DateTime.UtcNow });
        context.QualityInspections.Add(new QualityInspection { Id = 1, Code = "QI-001", BatchNumber = "B1", WorkOrderId = 1, ProcessStepId = 1, InspectorId = "qc", InspectorName = "李四", InspectedQuantity = 10, PassedQuantity = 9, FailedQuantity = 1, Status = InspectionStatus.Fail, InspectionTime = DateTime.UtcNow, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
        var service = new QualityService(context);

        var result = await service.TraceBatchAsync("B1");

        Assert.Equal("B1", result.BatchNumber);
        Assert.Single(result.ProductionReports);
        Assert.Single(result.Inspections);
    }

    [Fact]
    public async Task AnalyzeDefectsAsync_ShouldGroupDefectsByType()
    {
        await using var context = CreateContext();
        context.DefectTypes.Add(new DefectType { Id = 1, Code = "D001", Name = "尺寸超差" });
        context.QualityInspections.Add(new QualityInspection { Id = 1, Code = "QI-001", BatchNumber = "B1", WorkOrderId = 1, ProcessStepId = 1, InspectorId = "qc", InspectorName = "李四", InspectedQuantity = 10, PassedQuantity = 8, FailedQuantity = 2, Status = InspectionStatus.Fail, InspectionTime = DateTime.UtcNow, CreatedAt = DateTime.UtcNow });
        context.DefectRecords.Add(new DefectRecord { QualityInspectionId = 1, DefectTypeId = 1, Quantity = 2, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
        var service = new QualityService(context);

        var result = (await service.AnalyzeDefectsAsync()).ToList();

        Assert.Single(result);
        Assert.Equal("尺寸超差", result[0].DefectTypeName);
        Assert.Equal(2, result[0].TotalQuantity);
    }

    private static MesDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MesDbContext(options);
    }
}
