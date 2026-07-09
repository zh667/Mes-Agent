using MesCopilot.Agent.Plugins.ProductionAgentPlugin;
using MesCopilot.Agent.Plugins.ProductionAgentPlugin.Tools;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Agent.Plugins;

public class ProductionAgentPluginTests
{
    [Fact]
    public async Task GetTodayWorkOrdersTool_ShouldReturnCountsAndDebugInfo()
    {
        var service = new FakeWorkOrderService
        {
            TodayWorkOrders =
            [
                CreateWorkOrder(1, "WO-1", WorkOrderStatus.InProgress),
                CreateWorkOrder(2, "WO-2", WorkOrderStatus.Completed)
            ]
        };
        var tool = new GetTodayWorkOrdersTool(service);

        var result = await tool.ExecuteAsync(debugMode: true);

        Assert.NotNull(result.Data);
        Assert.Contains("2", result.Explanation);
        Assert.Equal("GetTodayWorkOrders", result.Debug?.ToolsCalled?.Single());
    }

    [Fact]
    public async Task GetTodayWorkOrdersTool_InvalidProductionLineId_ShouldThrow()
    {
        var tool = new GetTodayWorkOrdersTool(new FakeWorkOrderService());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => tool.ExecuteAsync(0));
    }

    [Fact]
    public async Task AnalyzeDelayedOrdersTool_ShouldReturnDelaySummary()
    {
        var service = new FakeWorkOrderService
        {
            DelayedWorkOrders =
            [
                CreateWorkOrder(1, "WO-DELAYED", WorkOrderStatus.InProgress, DateTime.UtcNow.AddDays(-2))
            ]
        };
        var tool = new AnalyzeDelayedOrdersTool(service);

        var result = await tool.ExecuteAsync(debugMode: true);

        Assert.NotNull(result.Data);
        Assert.Contains("1", result.Explanation);
        Assert.Equal("AnalyzeDelayedOrders", result.Debug?.ToolsCalled?.Single());
    }

    [Fact]
    public async Task AnalyzeDelayedOrdersTool_InvalidDateRange_ShouldThrow()
    {
        var tool = new AnalyzeDelayedOrdersTool(new FakeWorkOrderService());

        await Assert.ThrowsAsync<ArgumentException>(() => tool.ExecuteAsync(DateTime.UtcNow, DateTime.UtcNow.AddDays(-1)));
    }

    [Fact]
    public void ProductionAgentPlugin_ShouldExposeProductionTools()
    {
        var plugin = new ProductionAgentPlugin(new FakeWorkOrderService());

        Assert.Equal("ProductionAgent", plugin.Name);
        Assert.NotNull(plugin.GetTodayWorkOrdersTool);
        Assert.NotNull(plugin.AnalyzeDelayedOrdersTool);
    }

    private static WorkOrderDto CreateWorkOrder(
        int id,
        string code,
        WorkOrderStatus status,
        DateTime? plannedEndTime = null)
    {
        return new WorkOrderDto(
            id,
            code,
            1,
            "Product A",
            1,
            "Line 1",
            100,
            status == WorkOrderStatus.Completed ? 100 : 30,
            status == WorkOrderStatus.Completed ? 98 : 29,
            status,
            DateTime.UtcNow.AddHours(-1),
            plannedEndTime ?? DateTime.UtcNow.AddHours(8),
            DateTime.UtcNow.AddHours(-1),
            null,
            status == WorkOrderStatus.Completed ? 1m : 0.3m);
    }

    private sealed class FakeWorkOrderService : IWorkOrderService
    {
        public IReadOnlyList<WorkOrderDto> TodayWorkOrders { get; init; } = [];

        public IReadOnlyList<WorkOrderDto> DelayedWorkOrders { get; init; } = [];

        public Task<IEnumerable<WorkOrderDto>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<WorkOrderDto>>([]);
        }

        public Task<WorkOrderDto?> GetByIdAsync(int id)
        {
            return Task.FromResult<WorkOrderDto?>(null);
        }

        public Task<WorkOrderDto> CreateAsync(CreateWorkOrderRequest request)
        {
            throw new NotSupportedException();
        }

        public Task<WorkOrderDto> StartAsync(int id)
        {
            throw new NotSupportedException();
        }

        public Task<WorkOrderDto> ReportAsync(int id, ReportProductionRequest request)
        {
            throw new NotSupportedException();
        }

        public Task<WorkOrderDto> CompleteAsync(int id)
        {
            throw new NotSupportedException();
        }

        public Task<IEnumerable<WorkOrderDto>> GetTodayWorkOrdersAsync(int? productionLineId = null)
        {
            return Task.FromResult<IEnumerable<WorkOrderDto>>(TodayWorkOrders);
        }

        public Task<IEnumerable<WorkOrderDto>> GetDelayedWorkOrdersAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            return Task.FromResult<IEnumerable<WorkOrderDto>>(DelayedWorkOrders);
        }
    }
}
