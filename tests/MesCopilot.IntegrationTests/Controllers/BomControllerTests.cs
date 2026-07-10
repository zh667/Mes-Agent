using System.Net;
using System.Net.Http.Json;
using MesCopilot.Api.Dtos.Bom;
using MesCopilot.Application.Dtos.Bom;
using MesCopilot.Domain.Entities.Products;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using BomEntity = MesCopilot.Domain.Entities.Products.Bom;

namespace MesCopilot.IntegrationTests.Controllers;

public class BomControllerTests : IClassFixture<WorkOrdersApiFactory>
{
    private readonly WorkOrdersApiFactory _factory;

    public BomControllerTests(WorkOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Explode_ReturnsTenantScopedMaterialRequirements()
    {
        int productId = await SeedBomAsync();
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/bom/explode", new BomExplosionRequest
        {
            ProductId = productId,
            Quantity = 2m
        });

        response.EnsureSuccessStatusCode();
        BomExplosionResultDto? result = await response.Content.ReadFromJsonAsync<BomExplosionResultDto>();
        Assert.NotNull(result);
        Assert.Equal(6m, Assert.Single(result.TotalMaterials).RequiredQuantity);
    }

    [Fact]
    public async Task Explode_QuantityAboveLimitReturnsBadRequest()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/bom/explode", new BomExplosionRequest
        {
            ProductId = 1,
            Quantity = 1_000_001m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<int> SeedBomAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        CurrentTenantContext tenant = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
        tenant.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: false));
        MesDbContext context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
        string suffix = Guid.NewGuid().ToString("N")[..8];
        Product product = new() { Code = $"BOM-{suffix}", Name = "BOM Test Product", Unit = "set" };
        Material material = new() { Code = $"MAT-{suffix}", Name = "BOM Test Material", Unit = "pcs" };
        BomEntity bom = new() { Code = $"B-{suffix}", Product = product, IsActive = true };
        bom.BomItems.Add(new BomItem { Material = material, Quantity = 3m, Unit = "pcs" });
        context.Boms.Add(bom);
        await context.SaveChangesAsync();
        return product.Id;
    }
}
