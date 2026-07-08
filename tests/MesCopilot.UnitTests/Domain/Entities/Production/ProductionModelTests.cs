using MesCopilot.Domain.Entities.Production;

namespace MesCopilot.UnitTests.Domain.Entities.Production;

public class ProductionModelTests
{
    [Fact]
    public void ProductionLine_ShouldDefaultToActiveAndInitializeWorkOrders()
    {
        var line = new ProductionLine();

        Assert.True(line.IsActive);
        Assert.NotNull(line.WorkOrders);
        Assert.Empty(line.WorkOrders);
    }

    [Fact]
    public void ProcessRoute_ShouldDefaultToActiveVersionOneAndInitializeSteps()
    {
        var route = new ProcessRoute();

        Assert.True(route.IsActive);
        Assert.Equal("1.0", route.Version);
        Assert.NotNull(route.ProcessSteps);
        Assert.Empty(route.ProcessSteps);
    }

    [Fact]
    public void Workstation_ShouldDefaultToActiveAndInitializeProcessSteps()
    {
        var workstation = new Workstation();

        Assert.True(workstation.IsActive);
        Assert.NotNull(workstation.ProcessSteps);
        Assert.Empty(workstation.ProcessSteps);
    }

    [Fact]
    public void ProductionReport_ShouldKeepTraceabilityFields()
    {
        var timestamp = DateTime.UtcNow;
        var report = new ProductionReport
        {
            BatchNumber = "B20260708",
            WorkOrderId = 1,
            ProcessStepId = 2,
            EquipmentId = 3,
            OperatorId = "op-1",
            OperatorName = "张三",
            Timestamp = timestamp,
            Quantity = 100,
            QualifiedQuantity = 98
        };

        Assert.Equal("B20260708", report.BatchNumber);
        Assert.Equal(1, report.WorkOrderId);
        Assert.Equal(2, report.ProcessStepId);
        Assert.Equal(3, report.EquipmentId);
        Assert.Equal("op-1", report.OperatorId);
        Assert.Equal("张三", report.OperatorName);
        Assert.Equal(timestamp, report.Timestamp);
        Assert.Equal(100, report.Quantity);
        Assert.Equal(98, report.QualifiedQuantity);
    }
}
