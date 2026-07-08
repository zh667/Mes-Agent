using MesCopilot.Agent.Plugins.OeeAgentPlugin;
using MesCopilot.Agent.Plugins.OeeAgentPlugin.Tools;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Agent.Plugins;

public class OeeAgentPluginTests
{
    [Fact]
    public async Task CalculateOeeTool_ShouldReturnOeeMetrics()
    {
        var service = new FakeEquipmentService();
        var tool = new CalculateOeeTool(service);

        var result = await tool.ExecuteAsync(1, DateTime.UtcNow.Date, debugMode: true);

        Assert.NotNull(result.Data);
        Assert.Contains("75", result.Explanation);
        Assert.Equal("CalculateOee", result.Debug?.ToolsCalled?.Single());
    }

    [Fact]
    public async Task CalculateOeeTool_InvalidEquipmentId_ShouldThrow()
    {
        var tool = new CalculateOeeTool(new FakeEquipmentService());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => tool.ExecuteAsync(0, DateTime.UtcNow.Date));
    }

    [Fact]
    public async Task CalculateOeeTool_FutureDate_ShouldThrow()
    {
        var tool = new CalculateOeeTool(new FakeEquipmentService());

        await Assert.ThrowsAsync<ArgumentException>(() => tool.ExecuteAsync(1, DateTime.UtcNow.Date.AddDays(1)));
    }

    [Fact]
    public async Task AnalyzeLowOeeTool_ShouldReturnLowOeeEquipment()
    {
        var service = new FakeEquipmentService();
        var tool = new AnalyzeLowOeeTool(service);

        var result = await tool.ExecuteAsync(DateTime.UtcNow.Date, 0.8m, debugMode: true);

        Assert.NotNull(result.Data);
        Assert.Contains("1", result.Explanation);
        Assert.Equal("AnalyzeLowOee", result.Debug?.ToolsCalled?.Single());
    }

    [Fact]
    public async Task AnalyzeLowOeeTool_InvalidThreshold_ShouldThrow()
    {
        var tool = new AnalyzeLowOeeTool(new FakeEquipmentService());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => tool.ExecuteAsync(DateTime.UtcNow.Date, 1.1m));
    }

    [Fact]
    public async Task GetEquipmentStatusTool_ShouldReturnStatusAndAlarms()
    {
        var service = new FakeEquipmentService();
        var tool = new GetEquipmentStatusTool(service);

        var result = await tool.ExecuteAsync(1, debugMode: true);

        Assert.NotNull(result.Data);
        Assert.Contains("EQ-1", result.Explanation);
        Assert.Equal("GetEquipmentStatus", result.Debug?.ToolsCalled?.Single());
    }

    [Fact]
    public async Task GetEquipmentStatusTool_InvalidEquipmentId_ShouldThrow()
    {
        var tool = new GetEquipmentStatusTool(new FakeEquipmentService());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => tool.ExecuteAsync(0));
    }

    [Fact]
    public void OeeAgentPlugin_ShouldExposeOeeTools()
    {
        var plugin = new OeeAgentPlugin(new FakeEquipmentService());

        Assert.Equal("OeeAgent", plugin.Name);
        Assert.NotNull(plugin.CalculateOeeTool);
        Assert.NotNull(plugin.AnalyzeLowOeeTool);
        Assert.NotNull(plugin.GetEquipmentStatusTool);
    }

    private sealed class FakeEquipmentService : IEquipmentService
    {
        public Task<IEnumerable<EquipmentDto>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<EquipmentDto>>(
            [
                new EquipmentDto(1, "EQ-1", "Machine 1", null, 1, "Line 1", 100, 1.0m, true)
            ]);
        }

        public Task<EquipmentDto?> GetByIdAsync(int id)
        {
            return Task.FromResult<EquipmentDto?>(new EquipmentDto(id, $"EQ-{id}", $"Machine {id}", null, 1, "Line 1", 100, 1.0m, true));
        }

        public Task<IEnumerable<EquipmentStatusDto>> GetStatusHistoryAsync(int equipmentId)
        {
            return Task.FromResult<IEnumerable<EquipmentStatusDto>>(
            [
                new EquipmentStatusDto(1, equipmentId, EquipmentState.Running, DateTime.UtcNow.AddMinutes(-10), null, null, null)
            ]);
        }

        public Task<IEnumerable<EquipmentAlarmDto>> GetAlarmsAsync(int equipmentId)
        {
            return Task.FromResult<IEnumerable<EquipmentAlarmDto>>(
            [
                new EquipmentAlarmDto(1, equipmentId, "A-001", "Sensor anomaly", 2, DateTime.UtcNow, null, null, null, null)
            ]);
        }

        public Task<OeeDto> CalculateOeeAsync(int equipmentId, DateTime date)
        {
            return Task.FromResult(new OeeDto(equipmentId, date, 0.9m, 0.9m, 0.93m, 0.75m, 100, 93, 480));
        }
    }
}
