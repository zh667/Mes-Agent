using MesCopilot.Domain.Entities.Products;

namespace MesCopilot.Domain.Entities.Production;

/// <summary>
/// 工艺路线。
/// </summary>
public class ProcessRoute
{
    public int Id { get; set; }

    /// <summary>
    /// 路线编号。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 路线名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 产品 ID。
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// 版本号。
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public Product Product { get; set; } = null!;

    public ICollection<ProcessStep> ProcessSteps { get; set; } = new List<ProcessStep>();
}
