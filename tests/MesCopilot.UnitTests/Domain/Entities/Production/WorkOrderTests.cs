using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Domain.Entities.Production;

public class WorkOrderTests
{
    [Fact]
    public void WorkOrder_ShouldCalculateProgress()
    {
        var workOrder = new WorkOrder
        {
            PlannedQuantity = 1000,
            CompletedQuantity = 750
        };

        var progress = workOrder.Progress;

        Assert.Equal(0.75m, progress);
    }

    [Fact]
    public void WorkOrder_Progress_ShouldReturnZero_WhenPlannedQuantityIsZero()
    {
        var workOrder = new WorkOrder
        {
            PlannedQuantity = 0,
            CompletedQuantity = 100
        };

        var progress = workOrder.Progress;

        Assert.Equal(0m, progress);
    }

    [Fact]
    public void WorkOrder_Progress_ShouldClampToOne_WhenCompletedQuantityExceedsPlan()
    {
        var workOrder = new WorkOrder
        {
            PlannedQuantity = 100,
            CompletedQuantity = 125
        };

        var progress = workOrder.Progress;

        Assert.Equal(1m, progress);
    }

    [Fact]
    public void WorkOrder_ShouldInitializeWithNotScheduledStatus()
    {
        var workOrder = new WorkOrder();

        Assert.Equal(WorkOrderStatus.NotScheduled, workOrder.Status);
    }

    [Fact]
    public void WorkOrder_ShouldInitializeCollections()
    {
        var workOrder = new WorkOrder();

        Assert.NotNull(workOrder.Operations);
        Assert.NotNull(workOrder.ProductionReports);
        Assert.Empty(workOrder.Operations);
        Assert.Empty(workOrder.ProductionReports);
    }
}
