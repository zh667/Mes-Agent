using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.UnitTests.Infrastructure.Data;

public class SeedDataTests
{
    [Fact]
    public async Task SeedAsync_ShouldCreateMvpReferenceData()
    {
        await using var context = CreateContext();

        await SeedData.SeedAsync(context);

        Assert.Equal(3, await context.Products.CountAsync());
        Assert.Equal(2, await context.ProductionLines.CountAsync());
        Assert.Equal(5, await context.Equipment.CountAsync());
        Assert.Equal(10, await context.WorkOrders.CountAsync());
        Assert.Equal(4, await context.DefectTypes.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_ShouldBeIdempotent_WhenProductsAlreadyExist()
    {
        await using var context = CreateContext();

        await SeedData.SeedAsync(context);
        await SeedData.SeedAsync(context);

        Assert.Equal(3, await context.Products.CountAsync());
        Assert.Equal(10, await context.WorkOrders.CountAsync());
    }

    private static MesDbContext CreateContext()
    {
        return Tenancy.TenantTestDbContextFactory.Create();
    }
}
