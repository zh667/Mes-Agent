using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MesCopilot.Api.Dtos.Auth;
using MesCopilot.Infrastructure.Data;
using MesCopilot.IntegrationTests.Controllers;

namespace MesCopilot.IntegrationTests.Hubs;

public sealed class EquipmentHubEndpointTests
{
    [Fact]
    public async Task Negotiate_AnonymousRequest_IsRejected()
    {
        await using AuthApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            $"/hubs/equipment/negotiate?negotiateVersion=1&tenantId={SeedData.DefaultTenantId}",
            content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Negotiate_ValidMembershipSucceedsAndUnknownTenantIsRejected()
    {
        await using AuthApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        string email = $"hub-{Guid.NewGuid():N}@example.test";
        HttpResponseMessage registration = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Hub-Test-Password1!",
            DisplayName = "Hub Test"
        });
        registration.EnsureSuccessStatusCode();
        AuthResponse auth = (await registration.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        HttpResponseMessage allowed = await client.PostAsync(
            $"/hubs/equipment/negotiate?negotiateVersion=1&tenantId={SeedData.DefaultTenantId}",
            content: null);
        HttpResponseMessage forbidden = await client.PostAsync(
            "/hubs/equipment/negotiate?negotiateVersion=1&tenantId=00000000-0000-0000-0000-000000000099",
            content: null);

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }
}
