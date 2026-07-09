using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Domain.Enums;

public class EquipmentStateTests
{
    [Theory]
    [InlineData(EquipmentState.Running, 0)]
    [InlineData(EquipmentState.Idle, 1)]
    [InlineData(EquipmentState.Alarm, 2)]
    [InlineData(EquipmentState.Maintenance, 3)]
    [InlineData(EquipmentState.Offline, 4)]
    public void EquipmentState_ShouldHaveCorrectValues(EquipmentState state, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)state);
    }
}
