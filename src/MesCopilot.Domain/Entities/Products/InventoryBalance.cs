using MesCopilot.Domain.Common;

namespace MesCopilot.Domain.Entities.Products;

public sealed class InventoryBalance : ITenantEntity
{
    public int Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public int MaterialId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Material Material { get; set; } = null!;

    public decimal AvailableQuantity => Math.Max(0m, QuantityOnHand - QuantityReserved);
}
