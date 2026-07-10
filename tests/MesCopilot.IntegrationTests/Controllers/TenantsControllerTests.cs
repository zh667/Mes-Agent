using System.Net;
using System.Net.Http.Json;
using MesCopilot.Api.Dtos.Tenants;
using MesCopilot.IntegrationTests.Tenancy;

namespace MesCopilot.IntegrationTests.Controllers;

public class TenantsControllerTests
{
    [Fact]
    public async Task Mine_ReturnsOnlyAuthenticatedUsersActiveMemberships()
    {
        await using TenantApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/tenants/mine");
        IReadOnlyList<TenantSummaryDto>? tenants = await response.Content.ReadFromJsonAsync<IReadOnlyList<TenantSummaryDto>>();

        response.EnsureSuccessStatusCode();
        TenantSummaryDto tenant = Assert.Single(tenants!);
        Assert.Equal(TenantApiFactory.TenantA, tenant.Id);
        Assert.Equal("TeamLead", tenant.Role);
    }

    [Fact]
    public async Task Create_WithoutPlatformAdminClaim_ReturnsForbidden()
    {
        await using TenantApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/tenants", new CreateTenantRequest("PLANT-B", "Plant B"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithPlatformAdminClaim_CreatesNormalizedTenant()
    {
        await using TenantApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Platform-Admin", "true");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/tenants",
            new CreateTenantRequest("plant-b", "Plant B"));
        string responseBody = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"Expected 201 Created but received {(int)response.StatusCode}: {responseBody}");
        TenantSummaryDto? tenant = await response.Content.ReadFromJsonAsync<TenantSummaryDto>();

        Assert.Equal("PLANT-B", tenant?.Code);
        Assert.True(tenant?.IsActive);
    }
}
