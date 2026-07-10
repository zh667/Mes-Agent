namespace MesCopilot.Domain.Entities.Production;

/// <summary>
/// 工序定义。
/// </summary>
public class ProcessStep : MesCopilot.Domain.Common.ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;

    public int Id { get; set; }

    /// <summary>
    /// 工序编号。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 工序名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工艺路线 ID。
    /// </summary>
    public int ProcessRouteId { get; set; }

    /// <summary>
    /// 工位 ID。
    /// </summary>
    public int? WorkstationId { get; set; }

    /// <summary>
    /// 序号。
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// 标准工时，单位分钟。
    /// </summary>
    public decimal StandardTime { get; set; }

    /// <summary>
    /// 描述。
    /// </summary>
    public string? Description { get; set; }

    public ProcessRoute ProcessRoute { get; set; } = null!;

    public Workstation? Workstation { get; set; }
}
