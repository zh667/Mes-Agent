using MesCopilot.Application.Dtos.Bom;
using MesCopilot.Domain.Entities.Products;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using BomEntity = MesCopilot.Domain.Entities.Products.Bom;

namespace MesCopilot.Application.Services.Bom;

public sealed class BomExplosionService : IBomExplosionService
{
    private const int MaxDepth = 20;
    private readonly MesDbContext _context;

    public BomExplosionService(MesDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BomProductOptionDto>> GetAvailableProductsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Boms
            .AsNoTracking()
            .Where(bom => bom.IsActive)
            .OrderBy(bom => bom.Product.Code)
            .Select(bom => new BomProductOptionDto(bom.ProductId, bom.Product.Code, bom.Product.Name, bom.Version))
            .ToListAsync(cancellationToken);
    }

    public async Task<BomExplosionResultDto> ExplodeAsync(
        int productId,
        decimal quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0m || quantity > 1_000_000m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero and no more than 1,000,000.");
        }

        List<BomEntity> boms = await _context.Boms
            .AsNoTracking()
            .Include(bom => bom.Product)
            .Where(bom => bom.IsActive)
            .ToListAsync(cancellationToken);
        BomEntity root = boms
            .Where(bom => bom.ProductId == productId)
            .OrderByDescending(bom => bom.UpdatedAt ?? bom.CreatedAt)
            .ThenByDescending(bom => bom.Id)
            .FirstOrDefault()
            ?? throw new BomNotFoundException($"No active BOM exists for product {productId}.");

        HashSet<int> bomIds = boms.Select(bom => bom.Id).ToHashSet();
        List<BomItem> items = await _context.BomItems
            .AsNoTracking()
            .Where(item => bomIds.Contains(item.BomId))
            .OrderBy(item => item.Sequence)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
        HashSet<int> materialIds = items
            .Where(item => item.MaterialId.HasValue)
            .Select(item => item.MaterialId!.Value)
            .ToHashSet();
        Dictionary<int, Material> materials = await _context.Materials
            .AsNoTracking()
            .Where(material => materialIds.Contains(material.Id))
            .ToDictionaryAsync(material => material.Id, cancellationToken);
        Dictionary<int, InventoryBalance> balances = await _context.InventoryBalances
            .AsNoTracking()
            .Where(balance => materialIds.Contains(balance.MaterialId))
            .ToDictionaryAsync(balance => balance.MaterialId, cancellationToken);

        Dictionary<int, BomEntity> bomsById = boms.ToDictionary(bom => bom.Id);
        Dictionary<int, List<BomItem>> itemsByBom = items
            .GroupBy(item => item.BomId)
            .ToDictionary(group => group.Key, group => group.ToList());
        List<BomExplosionItemDto> exploded = [];
        Expand(
            root,
            quantity,
            [root.Product.Code, root.Code],
            [],
            1,
            bomsById,
            itemsByBom,
            materials,
            balances,
            exploded);
        exploded = AllocateInventory(exploded, balances);

        List<BomMaterialSummaryDto> totals = exploded
            .GroupBy(item => item.MaterialId)
            .Select(group =>
            {
                BomExplosionItemDto first = group.First();
                decimal required = group.Sum(item => item.RequiredQuantity);
                decimal available = balances.TryGetValue(first.MaterialId, out InventoryBalance? balance)
                    ? balance.AvailableQuantity
                    : 0m;
                return new BomMaterialSummaryDto(
                    first.MaterialId,
                    first.MaterialCode,
                    first.MaterialName,
                    first.Unit,
                    required,
                    available,
                    Math.Max(0m, required - available));
            })
            .OrderBy(item => item.MaterialCode)
            .ToList();

        return new BomExplosionResultDto(
            root.ProductId,
            root.Product.Code,
            root.Product.Name,
            quantity,
            exploded,
            totals);
    }

    private static List<BomExplosionItemDto> AllocateInventory(
        IEnumerable<BomExplosionItemDto> items,
        IReadOnlyDictionary<int, InventoryBalance> balances)
    {
        Dictionary<int, decimal> remaining = balances.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.AvailableQuantity);
        List<BomExplosionItemDto> allocated = [];
        foreach (BomExplosionItemDto item in items)
        {
            decimal available = remaining.GetValueOrDefault(item.MaterialId);
            decimal assigned = Math.Min(item.RequiredQuantity, available);
            remaining[item.MaterialId] = available - assigned;
            allocated.Add(item with
            {
                AvailableQuantity = assigned,
                ShortageQuantity = item.RequiredQuantity - assigned
            });
        }

        return allocated;
    }

    private static void Expand(
        BomEntity bom,
        decimal multiplier,
        IReadOnlyList<string> path,
        IReadOnlyList<int> ancestors,
        int depth,
        IReadOnlyDictionary<int, BomEntity> bomsById,
        IReadOnlyDictionary<int, List<BomItem>> itemsByBom,
        IReadOnlyDictionary<int, Material> materials,
        IReadOnlyDictionary<int, InventoryBalance> balances,
        ICollection<BomExplosionItemDto> result)
    {
        if (depth > MaxDepth)
        {
            throw new BomDepthExceededException($"BOM depth exceeds {MaxDepth}: {string.Join(" -> ", path)}");
        }

        if (ancestors.Contains(bom.Id))
        {
            throw new BomCycleException($"BOM cycle detected: {string.Join(" -> ", path)}");
        }

        List<int> nextAncestors = [.. ancestors, bom.Id];
        if (!itemsByBom.TryGetValue(bom.Id, out List<BomItem>? children))
        {
            return;
        }

        foreach (BomItem item in children)
        {
            decimal required = multiplier * item.Quantity;
            if (item.MaterialId.HasValue)
            {
                if (!materials.TryGetValue(item.MaterialId.Value, out Material? material))
                {
                    throw new BomNotFoundException($"Material {item.MaterialId.Value} referenced by BOM {bom.Code} was not found.");
                }

                decimal available = balances.TryGetValue(material.Id, out InventoryBalance? balance)
                    ? balance.AvailableQuantity
                    : 0m;
                result.Add(new BomExplosionItemDto(
                    material.Id,
                    material.Code,
                    material.Name,
                    material.Unit,
                    required,
                    available,
                    Math.Max(0m, required - available),
                    depth,
                    [.. path, material.Code]));
                continue;
            }

            if (!item.ChildBomId.HasValue || !bomsById.TryGetValue(item.ChildBomId.Value, out BomEntity? childBom))
            {
                throw new BomNotFoundException($"Active child BOM {item.ChildBomId} referenced by {bom.Code} was not found.");
            }

            Expand(
                childBom,
                required,
                [.. path, childBom.Product.Code, childBom.Code],
                nextAncestors,
                depth + 1,
                bomsById,
                itemsByBom,
                materials,
                balances,
                result);
        }
    }
}
