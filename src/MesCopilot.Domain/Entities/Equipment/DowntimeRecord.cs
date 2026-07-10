namespace MesCopilot.Domain.Entities.Equipment;

/// <summary>
/// 停机记录。
/// </summary>
public class DowntimeRecord : MesCopilot.Domain.Common.ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;

    public int Id { get; set; }

    /// <summary>
    /// 设备 ID。
    /// </summary>
    public int EquipmentId { get; set; }

    /// <summary>
    /// 停机原因。
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 停机类型。
    /// </summary>
    public string DowntimeType { get; set; } = string.Empty;

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
    /// 描述。
    /// </summary>
    public string? Description { get; set; }

    public Equipment Equipment { get; set; } = null!;
}
