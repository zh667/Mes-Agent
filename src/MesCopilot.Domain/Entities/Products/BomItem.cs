namespace MesCopilot.Domain.Entities.Products;

/// <summary>
/// BOM 明细。
/// </summary>
public class BomItem
{
    public int Id { get; set; }

    /// <summary>
    /// BOM ID。
    /// </summary>
    public int BomId { get; set; }

    /// <summary>
    /// 物料 ID。
    /// </summary>
    public int MaterialId { get; set; }

    /// <summary>
    /// 用量。
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 单位。
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// 序号。
    /// </summary>
    public int Sequence { get; set; }

    public Bom Bom { get; set; } = null!;

    public Material Material { get; set; } = null!;
}
