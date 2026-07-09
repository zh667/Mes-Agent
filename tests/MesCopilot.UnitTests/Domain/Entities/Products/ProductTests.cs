using MesCopilot.Domain.Entities.Products;

namespace MesCopilot.UnitTests.Domain.Entities.Products;

public class ProductTests
{
    [Fact]
    public void Product_ShouldHaveRequiredProperties()
    {
        var createdAt = DateTime.UtcNow;

        var product = new Product
        {
            Id = 1,
            Code = "PROD-001",
            Name = "产品A",
            Specification = "规格说明",
            Unit = "个",
            CreatedAt = createdAt
        };

        Assert.Equal(1, product.Id);
        Assert.Equal("PROD-001", product.Code);
        Assert.Equal("产品A", product.Name);
        Assert.Equal("规格说明", product.Specification);
        Assert.Equal("个", product.Unit);
        Assert.Equal(createdAt, product.CreatedAt);
    }

    [Fact]
    public void Product_ShouldInitializeCollections()
    {
        var product = new Product();

        Assert.NotNull(product.Boms);
        Assert.Empty(product.Boms);
    }
}
