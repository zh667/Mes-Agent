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

    [Fact]
    public void DetermineState_InProgressWorkOrder_ShouldReturnOperationalState()
    {
        var calculator = new EquipmentStateCalculator();
        var workOrder = new WorkOrder { Status = WorkOrderStatus.InProgress };

        var state = calculator.DetermineState(CreateEquipment(), workOrder);

        Assert.Contains(
            state,
            new[]
            {
                EquipmentState.Alarm,
                EquipmentState.Maintenance,
                EquipmentState.Idle,
                EquipmentState.Running
            });
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
}
