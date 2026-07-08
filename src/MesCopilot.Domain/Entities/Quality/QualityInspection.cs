using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Enums;

namespace MesCopilot.Domain.Entities.Quality;

/// <summary>
/// 质检记录。
/// </summary>
public class QualityInspection
{
    public int Id { get; set; }

    /// <summary>
    /// 检验单号。
    /// </summary>
    public string Code { get; set; } = string.Empty;

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
    /// 检验员 ID。
    /// </summary>
    public string InspectorId { get; set; } = string.Empty;

    /// <summary>
    /// 检验员姓名。
    /// </summary>
    public string InspectorName { get; set; } = string.Empty;

    /// <summary>
    /// 检验数量。
    /// </summary>
    public int InspectedQuantity { get; set; }

    /// <summary>
    /// 合格数量。
    /// </summary>
    public int PassedQuantity { get; set; }

    /// <summary>
    /// 不合格数量。
    /// </summary>
    public int FailedQuantity { get; set; }

    /// <summary>
    /// 检验状态。
    /// </summary>
    public InspectionStatus Status { get; set; }

    /// <summary>
    /// 检验时间。
    /// </summary>
    public DateTime InspectionTime { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public WorkOrder WorkOrder { get; set; } = null!;

    public ProcessStep ProcessStep { get; set; } = null!;

    public ICollection<DefectRecord> DefectRecords { get; set; } = new List<DefectRecord>();
}
