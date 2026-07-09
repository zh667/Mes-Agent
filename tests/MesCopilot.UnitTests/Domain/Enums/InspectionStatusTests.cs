using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Domain.Enums;

public class InspectionStatusTests
{
    [Theory]
    [InlineData(InspectionStatus.Pending, 0)]
    [InlineData(InspectionStatus.Pass, 1)]
    [InlineData(InspectionStatus.Fail, 2)]
    public void InspectionStatus_ShouldHaveCorrectValues(InspectionStatus status, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)status);
    }
}
