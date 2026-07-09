using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Enums;
using EquipmentEntity = MesCopilot.Domain.Entities.Equipment.Equipment;

namespace MesCopilot.UnitTests.Domain.Entities.Equipment;

public class EquipmentModelTests
{
    [Fact]
    public void Equipment_ShouldDefaultToActiveAndInitializeHistoryCollections()
    {
        var equipment = new EquipmentEntity();

        Assert.True(equipment.IsActive);
        Assert.NotNull(equipment.StatusHistory);
        Assert.NotNull(equipment.Alarms);
        Assert.NotNull(equipment.DowntimeRecords);
        Assert.Empty(equipment.StatusHistory);
        Assert.Empty(equipment.Alarms);
        Assert.Empty(equipment.DowntimeRecords);
    }

    [Fact]
    public void EquipmentStatus_ShouldKeepStateDurationAndTimeRange()
    {
        var start = DateTime.UtcNow;
        var end = start.AddMinutes(15);

        var status = new EquipmentStatus
        {
            EquipmentId = 1,
            State = EquipmentState.Alarm,
            StartTime = start,
            EndTime = end,
            DurationMinutes = 15,
            Remarks = "A102 alarm"
        };

        Assert.Equal(1, status.EquipmentId);
        Assert.Equal(EquipmentState.Alarm, status.State);
        Assert.Equal(start, status.StartTime);
        Assert.Equal(end, status.EndTime);
        Assert.Equal(15, status.DurationMinutes);
        Assert.Equal("A102 alarm", status.Remarks);
    }
}
