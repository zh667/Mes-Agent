using System.Net;

namespace MesCopilot.IntegrationTests.Controllers;

public class HealthEndpointTests : IClassFixture<WorkOrdersApiFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WorkOrdersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
