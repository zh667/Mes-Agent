namespace MesCopilot.Domain.Entities.Products;

/// <summary>
/// 产品主数据。
/// </summary>
public class Product
{
    public int Id { get; set; }

    /// <summary>
    /// 产品编号。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 产品名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 规格说明。
    /// </summary>
    public string? Specification { get; set; }

    /// <summary>
    /// 单位。
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间。
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Bom> Boms { get; set; } = new List<Bom>();
}
