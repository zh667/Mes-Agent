using MesCopilot.Domain.Enums;

namespace MesCopilot.Domain.Entities.Equipment;

/// <summary>
/// 设备状态记录。
/// </summary>
public class EquipmentStatus
{
    public int Id { get; set; }

    /// <summary>
    /// 设备 ID。
    /// </summary>
    public int EquipmentId { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public EquipmentState State { get; set; }

    /// <summary>
    /// 开始时间。
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间。
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 持续时长，单位分钟。
    /// </summary>
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remarks { get; set; }

    public Equipment Equipment { get; set; } = null!;
}
