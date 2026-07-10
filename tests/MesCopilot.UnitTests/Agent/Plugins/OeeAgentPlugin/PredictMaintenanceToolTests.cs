using MesCopilot.Agent.Plugins.OeeAgentPlugin.Tools;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;

namespace MesCopilot.UnitTests.Agent.Plugins.OeeAgentPluginTools;

public class PredictMaintenanceToolTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsStructuredMaintenancePrediction()
    {
        PredictMaintenanceTool tool = new(new FakeMaintenancePredictionService());

        var result = await tool.ExecuteAsync(1, debugMode: true);

        Assert.NotNull(result.Data);
        Assert.Contains("EQ-001", result.Explanation);
        Assert.Contains("72.5", result.Explanation);
        Assert.Equal("PredictMaintenance", result.Debug?.ToolsCalled?.Single());
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidEquipmentId_ThrowsArgumentOutOfRangeException()
    {
        PredictMaintenanceTool tool = new(new FakeMaintenancePredictionService());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => tool.ExecuteAsync(0));
    }

    private sealed class FakeMaintenancePredictionService : IMaintenancePredictionService
    {
        public Task<MaintenancePredictionDto> PredictMaintenanceAsync(int equipmentId)
        {
            return Task.FromResult(new MaintenancePredictionDto
            {
                EquipmentId = equipmentId,
                EquipmentCode = "EQ-001",
                EquipmentName = "CNC 1",
                HealthScore = 72.5,
                HealthLevel = "Watch",
                MtbfHours = 48,
                MttrHours = 2,
                PredictedNextFailureAt = DateTime.UtcNow.AddDays(2),
                MaintenanceRecommendation = "Schedule preventive maintenance"
            });
        }
    }
}
