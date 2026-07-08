using MesCopilot.DeviceSimulator.Services;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.DeviceSimulator;

public class EquipmentStateCalculatorTests
{
    [Fact]
    public void DetermineState_NoActiveWorkOrder_ShouldReturnIdleOrOffline()
    {
        var calculator = new EquipmentStateCalculator();

        var state = calculator.DetermineState(CreateEquipment(), null);

        Assert.Contains(state, new[] { EquipmentState.Idle, EquipmentState.Offline });
    }

    [Fact]
    public void DetermineState_PausedWorkOrder_ShouldReturnIdleOrMaintenance()
    {
        var calculator = new EquipmentStateCalculator();
        var workOrder = new WorkOrder { Status = WorkOrderStatus.Paused };

        var state = calculator.DetermineState(CreateEquipment(), workOrder);

        Assert.Contains(state, new[] { EquipmentState.Idle, EquipmentState.Maintenance });
    }

    [Theory]
    [InlineData(4, EquipmentState.Alarm)]
    [InlineData(5, EquipmentState.Maintenance)]
    [InlineData(6, EquipmentState.Maintenance)]
    [InlineData(7, EquipmentState.Idle)]
    [InlineData(26, EquipmentState.Idle)]
    [InlineData(27, EquipmentState.Running)]
    public void DetermineState_InProgressWorkOrder_ShouldMapRollToExpectedState(
        int roll,
        EquipmentState expectedState)
    {
        var calculator = new EquipmentStateCalculator(new FixedNextRandom(roll));
        var workOrder = new WorkOrder { Status = WorkOrderStatus.InProgress };

        var state = calculator.DetermineState(CreateEquipment(), workOrder);

        Assert.Equal(expectedState, state);
    }

    [Fact]
    public void CalculateOutput_NotRunning_ShouldReturnZero()
    {
        var calculator = new EquipmentStateCalculator();

        var output = calculator.CalculateOutput(CreateEquipment(), EquipmentState.Idle);

        Assert.Equal(0, output);
    }

    [Fact]
    public void CalculateOutput_Running_ShouldUseRatedCapacity()
    {
        var calculator = new EquipmentStateCalculator();

        var output = calculator.CalculateOutput(CreateEquipment(), EquipmentState.Running);

        Assert.InRange(output, 9, 11);
    }

    private static Equipment CreateEquipment()
    {
        return new Equipment
        {
            Code = "EQ-TEST",
            Name = "Test Equipment",
            RatedCapacity = 3600
        };
    }

    private sealed class FixedNextRandom : Random
    {
        private readonly int _value;

        public FixedNextRandom(int value)
        {
            _value = value;
        }

        public override int Next(int maxValue)
        {
            Assert.InRange(_value, 0, maxValue - 1);
            return _value;
        }
    }
}
