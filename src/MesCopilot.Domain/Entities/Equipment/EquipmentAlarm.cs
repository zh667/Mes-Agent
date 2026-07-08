namespace MesCopilot.Domain.Entities.Equipment;

/// <summary>
/// 设备报警记录。
/// </summary>
public class EquipmentAlarm
{
    public int Id { get; set; }

    /// <summary>
    /// 设备 ID。
    /// </summary>
    public int EquipmentId { get; set; }

    /// <summary>
    /// 报警代码。
    /// </summary>
    public string AlarmCode { get; set; } = string.Empty;

    /// <summary>
    /// 报警消息。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 报警级别。
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 发生时间。
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// 确认时间。
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// 恢复时间。
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// 处理人员。
    /// </summary>
    public string? HandlerName { get; set; }

    /// <summary>
    /// 处理说明。
    /// </summary>
    public string? Resolution { get; set; }

    public Equipment Equipment { get; set; } = null!;
}
