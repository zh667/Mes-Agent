namespace MesCopilot.Domain.Entities.Production;

/// <summary>
/// 工位或工作中心。
/// </summary>
public class Workstation
{
    public int Id { get; set; }

    /// <summary>
    /// 工位编号。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 工位名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 产线 ID。
    /// </summary>
    public int ProductionLineId { get; set; }

    /// <summary>
    /// 描述。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool IsActive { get; set; } = true;

    public ProductionLine ProductionLine { get; set; } = null!;

    public ICollection<ProcessStep> ProcessSteps { get; set; } = new List<ProcessStep>();
}
