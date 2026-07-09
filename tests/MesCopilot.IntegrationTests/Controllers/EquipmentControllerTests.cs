using MesCopilot.Application.Dtos;
using System.Net.Http.Json;

namespace MesCopilot.IntegrationTests.Controllers;

public class EquipmentControllerTests : IClassFixture<WorkOrdersApiFactory>
{
    private readonly HttpClient _client;

    public EquipmentControllerTests(WorkOrdersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ShouldReturnEquipment()
    {
        var response = await _client.GetAsync("/api/equipment");

        response.EnsureSuccessStatusCode();
        var equipment = await response.Content.ReadFromJsonAsync<List<EquipmentDto>>();
        Assert.NotNull(equipment);
        Assert.NotEmpty(equipment);
    }

    [Fact]
    public async Task GetById_ExistingId_ShouldReturnEquipment()
    {
        var response = await _client.GetAsync("/api/equipment/1");

        response.EnsureSuccessStatusCode();
        var equipment = await response.Content.ReadFromJsonAsync<EquipmentDto>();
        Assert.NotNull(equipment);
        Assert.Equal(1, equipment.Id);
    }

    [Fact]
    public async Task CalculateOee_ShouldReturnMetrics()
    {
        var response = await _client.GetAsync($"/api/equipment/1/oee?date={DateTime.UtcNow:yyyy-MM-dd}");

        response.EnsureSuccessStatusCode();
        var oee = await response.Content.ReadFromJsonAsync<OeeDto>();
        Assert.NotNull(oee);
        Assert.Equal(1, oee.EquipmentId);
        Assert.InRange(oee.Oee, 0m, 1m);
    }
}
