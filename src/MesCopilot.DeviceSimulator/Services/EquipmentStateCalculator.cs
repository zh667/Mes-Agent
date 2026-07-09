using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Enums;

namespace MesCopilot.DeviceSimulator.Services;

public class EquipmentStateCalculator
{
    private readonly Random _random;

    public EquipmentStateCalculator()
        : this(new Random())
    {
    }

    public EquipmentStateCalculator(Random random)
    {
        _random = random;
    }

    public EquipmentState DetermineState(Equipment equipment, WorkOrder? activeWorkOrder)
    {
        if (activeWorkOrder is null)
        {
            return _random.Next(100) < 80 ? EquipmentState.Idle : EquipmentState.Offline;
        }

        if (activeWorkOrder.Status == WorkOrderStatus.Paused)
        {
            return _random.Next(100) < 70 ? EquipmentState.Idle : EquipmentState.Maintenance;
        }

        if (activeWorkOrder.Status == WorkOrderStatus.InProgress)
        {
            var roll = _random.Next(100);
            if (roll < 5)
            {
                return EquipmentState.Alarm;
            }

            if (roll < 7)
            {
                return EquipmentState.Maintenance;
            }

            if (roll < 27)
            {
                return EquipmentState.Idle;
            }

            return EquipmentState.Running;
        }

        return EquipmentState.Idle;
    }

    public int CalculateOutput(Equipment equipment, EquipmentState state)
    {
        if (state != EquipmentState.Running)
        {
            return 0;
        }

        var baseOutput = equipment.RatedCapacity / 360.0;
        var variance = _random.NextDouble() * 0.2 - 0.1;
        return (int)Math.Max(0, baseOutput * (1 + variance));
    }
}
