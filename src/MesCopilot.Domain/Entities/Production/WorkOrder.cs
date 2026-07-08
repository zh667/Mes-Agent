using MesCopilot.Domain.Entities.Products;
using MesCopilot.Domain.Enums;

namespace MesCopilot.Domain.Entities.Production;

/// <summary>
/// 生产工单。
/// </summary>
public class WorkOrder
{
    public int Id { get; set; }

    /// <summary>
    /// 工单编号。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 产品 ID。
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// 产线 ID。
    /// </summary>
    public int ProductionLineId { get; set; }

    /// <summary>
    /// 计划数量。
    /// </summary>
    public int PlannedQuantity { get; set; }

    /// <summary>
    /// 完工数量。
    /// </summary>
    public int CompletedQuantity { get; set; }

    /// <summary>
    /// 合格数量。
    /// </summary>
    public int QualifiedQuantity { get; set; }

    /// <summary>
    /// 工单状态。
    /// </summary>
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.NotScheduled;

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
    /// 创建时间。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间。
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 进度，范围按完成数量和计划数量计算。
    /// </summary>
    public decimal Progress => PlannedQuantity > 0
        ? (decimal)CompletedQuantity / PlannedQuantity
        : 0m;

    public Product Product { get; set; } = null!;

    public ProductionLine ProductionLine { get; set; } = null!;

    public ICollection<WorkOrderOperation> Operations { get; set; } = new List<WorkOrderOperation>();

    public ICollection<ProductionReport> ProductionReports { get; set; } = new List<ProductionReport>();
}
