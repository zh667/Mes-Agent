using MesCopilot.Application.Services.Bom;
using MesCopilot.Domain.Entities.Products;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;
using MesCopilot.Infrastructure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using BomEntity = MesCopilot.Domain.Entities.Products.Bom;

namespace MesCopilot.UnitTests.Application.Services.Bom;

public class BomExplosionServiceTests
{
    [Fact]
    public async Task ExplodeAsync_ExpandsThreeLevelsAggregatesPathsAndCalculatesShortage()
    {
        await using MesDbContext context = CreateContext();
        (Product root, BomEntity first, BomEntity second, BomEntity third, Material material) = SeedThreeLevels(context);
        first.BomItems.Add(new BomItem { ChildBom = second, Quantity = 2m, Unit = "set", Sequence = 1 });
        first.BomItems.Add(new BomItem { Material = material, Quantity = 1m, Unit = "pcs", Sequence = 2 });
        second.BomItems.Add(new BomItem { ChildBom = third, Quantity = 3m, Unit = "set", Sequence = 1 });
        third.BomItems.Add(new BomItem { Material = material, Quantity = 4m, Unit = "pcs", Sequence = 1 });
        context.InventoryBalances.Add(new InventoryBalance { Material = material, QuantityOnHand = 10m, QuantityReserved = 2m });
        await context.SaveChangesAsync();
        BomExplosionService service = new(context);

        var result = await service.ExplodeAsync(root.Id, 1m);

        Assert.Equal(2, result.Items.Count);
        var total = Assert.Single(result.TotalMaterials);
        Assert.Equal(25m, total.RequiredQuantity);
        Assert.Equal(8m, total.AvailableQuantity);
        Assert.Equal(17m, total.ShortageQuantity);
        Assert.Equal(8m, result.Items.Sum(item => item.AvailableQuantity));
        Assert.Equal(17m, result.Items.Sum(item => item.ShortageQuantity));
        Assert.Contains(result.Items, item => item.Depth == 3 && item.Path.Count == 7 && item.RequiredQuantity == 24m);
    }

    [Fact]
    public async Task ExplodeAsync_CycleIncludesBomPath()
    {
        await using MesDbContext context = CreateContext();
        Product firstProduct = new() { Code = "P1", Name = "P1", Unit = "set" };
        Product secondProduct = new() { Code = "P2", Name = "P2", Unit = "set" };
        BomEntity first = new() { Code = "B1", Product = firstProduct, IsActive = true };
        BomEntity second = new() { Code = "B2", Product = secondProduct, IsActive = true };
        first.BomItems.Add(new BomItem { ChildBom = second, Quantity = 1m, Unit = "set" });
        second.BomItems.Add(new BomItem { ChildBom = first, Quantity = 1m, Unit = "set" });
        context.Boms.AddRange(first, second);
        await context.SaveChangesAsync();

        BomCycleException exception = await Assert.ThrowsAsync<BomCycleException>(
            () => new BomExplosionService(context).ExplodeAsync(firstProduct.Id, 1m));

        Assert.Contains("B1", exception.Message, StringComparison.Ordinal);
        Assert.Contains("B2", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ExplodeAsync_NonPositiveQuantityIsRejected(decimal quantity)
    {
        await using MesDbContext context = CreateContext();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => new BomExplosionService(context).ExplodeAsync(1, quantity));
    }

    private static (Product, BomEntity, BomEntity, BomEntity, Material) SeedThreeLevels(MesDbContext context)
    {
        Product p1 = new() { Code = "P1", Name = "Assembly", Unit = "set" };
        Product p2 = new() { Code = "P2", Name = "Module", Unit = "set" };
        Product p3 = new() { Code = "P3", Name = "Submodule", Unit = "set" };
        BomEntity b1 = new() { Code = "B1", Product = p1, IsActive = true };
        BomEntity b2 = new() { Code = "B2", Product = p2, IsActive = true };
        BomEntity b3 = new() { Code = "B3", Product = p3, IsActive = true };
        Material material = new() { Code = "M1", Name = "Bearing", Unit = "pcs" };
        context.Boms.AddRange(b1, b2, b3);
        context.Materials.Add(material);
        return (p1, b1, b2, b3, material);
    }

    private static MesDbContext CreateContext()
    {
        CurrentTenantContext tenant = new();
        tenant.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: false));
        DbContextOptions<MesDbContext> options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase($"bom-{Guid.NewGuid():N}")
            .AddInterceptors(new TenantWriteGuardInterceptor(tenant))
            .Options;
        return new MesDbContext(options, tenant);
    }
}
