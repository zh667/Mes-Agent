using MesCopilot.Domain.Entities.Products;

namespace MesCopilot.UnitTests.Domain.Entities.Products;

public class ProductMaterialBomTests
{
    [Fact]
    public void Material_ShouldInitializeBomItems()
    {
        var material = new Material();

        Assert.NotNull(material.BomItems);
        Assert.Empty(material.BomItems);
    }

    [Fact]
    public void Bom_ShouldDefaultToActiveVersionOneAndInitializeItems()
    {
        var bom = new Bom();

        Assert.Equal("1.0", bom.Version);
        Assert.True(bom.IsActive);
        Assert.NotNull(bom.BomItems);
        Assert.Empty(bom.BomItems);
    }

    [Fact]
    public void BomItem_ShouldKeepMaterialUsageFields()
    {
        var item = new BomItem
        {
            BomId = 10,
            MaterialId = 20,
            Quantity = 2.5m,
            Unit = "kg",
            Sequence = 1
        };

        Assert.Equal(10, item.BomId);
        Assert.Equal(20, item.MaterialId);
        Assert.Equal(2.5m, item.Quantity);
        Assert.Equal("kg", item.Unit);
        Assert.Equal(1, item.Sequence);
    }
}
