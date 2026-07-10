using MesCopilot.Domain.Entities.Production;

namespace MesCopilot.Domain.Entities.Equipment;

/// <summary>
/// 设备主数据。
/// </summary>
public class Equipment : MesCopilot.Domain.Common.ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;

    public int Id { get; set; }

    /// <summary>
    /// 设备编号。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 设备型号。
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 产线 ID。
    /// </summary>
    public int? ProductionLineId { get; set; }

    public int? WorkstationId { get; set; }

    /// <summary>
    /// 额定产能。
    /// </summary>
    public int RatedCapacity { get; set; }

    /// <summary>
    /// 理想节拍，单位秒/件。
    /// </summary>
    public decimal IdealCycleTime { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public ProductionLine? ProductionLine { get; set; }

    public Workstation? Workstation { get; set; }

    public ICollection<EquipmentStatus> StatusHistory { get; set; } = new List<EquipmentStatus>();

    public ICollection<EquipmentAlarm> Alarms { get; set; } = new List<EquipmentAlarm>();

    public ICollection<DowntimeRecord> DowntimeRecords { get; set; } = new List<DowntimeRecord>();

    public ICollection<DeviceConnection> DeviceConnections { get; set; } = new List<DeviceConnection>();
}
