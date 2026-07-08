using System.Net;

namespace MesCopilot.IntegrationTests.Hubs;

public class EquipmentHubTests : IClassFixture<Controllers.WorkOrdersApiFactory>
{
    private readonly HttpClient _client;

    public EquipmentHubTests(Controllers.WorkOrdersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Negotiate_ShouldReturnSuccess()
    {
        var response = await _client.PostAsync("/hubs/equipment/negotiate?negotiateVersion=1", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
