namespace MesCopilot.Domain.Entities.Production;

/// <summary>
/// 工单工序。
/// </summary>
public class WorkOrderOperation : MesCopilot.Domain.Common.ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;

    public int Id { get; set; }

    /// <summary>
    /// 工单 ID。
    /// </summary>
    public int WorkOrderId { get; set; }

    /// <summary>
    /// 工序 ID。
    /// </summary>
    public int ProcessStepId { get; set; }

    public int? EquipmentId { get; set; }

    /// <summary>
    /// 序号。
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// 计划开始时间。
    /// </summary>
    public DateTime PlannedStartTime { get; set; }

    /// <summary>
    /// 计划结束时间。
    /// </summary>
    public DateTime PlannedEndTime { get; set; }

    /// <summary>
    /// 实际开始时间。
    /// </summary>
    public DateTime? ActualStartTime { get; set; }

    /// <summary>
    /// 实际结束时间。
    /// </summary>
    public DateTime? ActualEndTime { get; set; }

    /// <summary>
    /// 完工数量。
    /// </summary>
    public int CompletedQuantity { get; set; }

    public uint Version { get; private set; }

    public WorkOrder WorkOrder { get; set; } = null!;

    public ProcessStep ProcessStep { get; set; } = null!;

    public MesCopilot.Domain.Entities.Equipment.Equipment? Equipment { get; set; }
}
