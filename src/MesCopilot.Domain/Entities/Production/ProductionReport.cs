namespace MesCopilot.Domain.Entities.Production;

/// <summary>
/// 报工记录。
/// </summary>
public class ProductionReport
{
    public int Id { get; set; }

    /// <summary>
    /// 批次号。
    /// </summary>
    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>
    /// 工单 ID。
    /// </summary>
    public int WorkOrderId { get; set; }

    /// <summary>
    /// 工序 ID。
    /// </summary>
    public int ProcessStepId { get; set; }

    /// <summary>
    /// 设备 ID。
    /// </summary>
    public int EquipmentId { get; set; }

    /// <summary>
    /// 操作人员 ID。
    /// </summary>
    public string OperatorId { get; set; } = string.Empty;

    /// <summary>
    /// 操作人员姓名。
    /// </summary>
    public string OperatorName { get; set; } = string.Empty;

    /// <summary>
    /// 报工时间。
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 数量。
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 合格数量。
    /// </summary>
    public int QualifiedQuantity { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public WorkOrder WorkOrder { get; set; } = null!;

    public ProcessStep ProcessStep { get; set; } = null!;
}
