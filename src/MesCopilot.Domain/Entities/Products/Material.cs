namespace MesCopilot.Domain.Entities.Products;

/// <summary>
/// 物料主数据。
/// </summary>
public class Material : MesCopilot.Domain.Common.ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;

    public int Id { get; set; }

    /// <summary>
    /// 物料编号。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 物料名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 规格型号。
    /// </summary>
    public string? Specification { get; set; }

    /// <summary>
    /// 单位。
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// 库存数量。
    /// </summary>
    public decimal StockQuantity { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间。
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public ICollection<BomItem> BomItems { get; set; } = new List<BomItem>();

    public InventoryBalance? InventoryBalance { get; set; }
}
