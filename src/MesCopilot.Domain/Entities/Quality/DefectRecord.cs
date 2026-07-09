namespace MesCopilot.Domain.Entities.Quality;

/// <summary>
/// 不良记录。
/// </summary>
public class DefectRecord
{
    public int Id { get; set; }

    /// <summary>
    /// 质检记录 ID。
    /// </summary>
    public int QualityInspectionId { get; set; }

    /// <summary>
    /// 不良类型 ID。
    /// </summary>
    public int DefectTypeId { get; set; }

    /// <summary>
    /// 不良数量。
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 处理方式。
    /// </summary>
    public string? DisposalMethod { get; set; }

    /// <summary>
    /// 描述。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public QualityInspection QualityInspection { get; set; } = null!;

    public DefectType DefectType { get; set; } = null!;
}
