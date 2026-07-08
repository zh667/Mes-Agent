using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Domain.Enums;

public class WorkOrderStatusTests
{
    [Fact]
    public void WorkOrderStatus_ShouldHaveAllRequiredValues()
    {
        var values = Enum.GetValues<WorkOrderStatus>();

        Assert.Contains(WorkOrderStatus.NotScheduled, values);
        Assert.Contains(WorkOrderStatus.Scheduled, values);
        Assert.Contains(WorkOrderStatus.InProgress, values);
        Assert.Contains(WorkOrderStatus.Paused, values);
        Assert.Contains(WorkOrderStatus.Completed, values);
        Assert.Contains(WorkOrderStatus.Closed, values);
    }

    [Theory]
    [InlineData(WorkOrderStatus.NotScheduled, 0)]
    [InlineData(WorkOrderStatus.Scheduled, 1)]
    [InlineData(WorkOrderStatus.InProgress, 2)]
    [InlineData(WorkOrderStatus.Paused, 3)]
    [InlineData(WorkOrderStatus.Completed, 4)]
    [InlineData(WorkOrderStatus.Closed, 5)]
    public void WorkOrderStatus_ShouldHaveCorrectValues(WorkOrderStatus status, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)status);
    }
}
