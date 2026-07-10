namespace MesCopilot.Domain.Entities.Products;

/// <summary>
/// 物料清单。
/// </summary>
public class Bom : MesCopilot.Domain.Common.ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;

    public int Id { get; set; }

    /// <summary>
    /// BOM 编号。
    /// </summary>
    public string Code { get; set; } = string.Empty;

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

    /// <summary>
    /// 更新时间。
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public Product Product { get; set; } = null!;

    public ICollection<BomItem> BomItems { get; set; } = new List<BomItem>();

    public ICollection<BomItem> ParentBomItems { get; set; } = new List<BomItem>();
}
