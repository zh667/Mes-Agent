using System.Net;
using System.Net.Http.Json;
using MesCopilot.IntegrationTests.Controllers;

namespace MesCopilot.IntegrationTests.Health;

public sealed class Phase3DependencyHealthTests : IClassFixture<WorkOrdersApiFactory>
{
    private readonly HttpClient _client;

    public Phase3DependencyHealthTests(WorkOrdersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_WhenDatabaseAndCacheAvailable_ReturnsHealthyContract()
    {
        HttpResponseMessage response = await _client.GetAsync("/health");
        Dictionary<string, string>? payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", payload!["status"]);
    }
}
