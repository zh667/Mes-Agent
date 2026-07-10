using System.Text.Json;
using MesCopilot.Agent.Plugins.ProductionAgentPlugin.Tools;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Agent.Plugins.ProductionAgentPluginTools;

public class SuggestScheduleToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithSchedulableOrders_ReturnsEarliestDeadlineFirstOrder()
    {
        SuggestScheduleTool tool = new(
            new FakeWorkOrderService(
            [
                CreateWorkOrder(1, "WO-LATE", WorkOrderStatus.NotScheduled, DateTime.UtcNow.AddDays(1), 30),
                CreateWorkOrder(2, "WO-LATER", WorkOrderStatus.Scheduled, DateTime.UtcNow.AddDays(5), 10)
            ]),
            new FakeEquipmentService());

        var result = await tool.ExecuteAsync(productionLineId: null, startDate: DateTime.UtcNow, endDate: DateTime.UtcNow.AddDays(7), debugMode: true);

        string json = JsonSerializer.Serialize(result.Data);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement scheduledOrders = document.RootElement.GetProperty("scheduledOrders");

        Assert.Equal(2, document.RootElement.GetProperty("totalOrders").GetInt32());
        Assert.Equal("WO-LATE", scheduledOrders[0].GetProperty("workOrderCode").GetString());
        Assert.Contains("EDF", result.Explanation);
        Assert.Equal("SuggestSchedule", result.Debug?.ToolsCalled?.Single());
    }

    [Fact]
    public async Task ExecuteAsync_WithNoOrders_ReturnsEmptySchedule()
    {
        SuggestScheduleTool tool = new(new FakeWorkOrderService([]), new FakeEquipmentService());

        var result = await tool.ExecuteAsync(null, null, null);

        Assert.Contains("No schedulable work orders", result.Explanation);
    }

    private static WorkOrderDto CreateWorkOrder(
        int id,
        string code,
        WorkOrderStatus status,
        DateTime plannedEndTime,
        int plannedQuantity)
    {
        return new WorkOrderDto(
            id,
            code,
            1,
            "Product",
            1,
            "Line 1",
            plannedQuantity,
            0,
            0,
            status,
            DateTime.UtcNow,
            plannedEndTime,
            null,
            null,
            0m);
    }

    private sealed class FakeWorkOrderService : IWorkOrderService
    {
        private readonly IReadOnlyList<WorkOrderDto> _workOrders;

        public FakeWorkOrderService(IReadOnlyList<WorkOrderDto> workOrders)
        {
            _workOrders = workOrders;
        }

        public Task<IEnumerable<WorkOrderDto>> GetAllAsync() => Task.FromResult<IEnumerable<WorkOrderDto>>(_workOrders);

        public Task<WorkOrderDto?> GetByIdAsync(int id) => Task.FromResult<WorkOrderDto?>(null);

        public Task<WorkOrderDto> CreateAsync(CreateWorkOrderRequest request) => throw new NotSupportedException();

        public Task<WorkOrderDto> StartAsync(int id) => throw new NotSupportedException();

        public Task<WorkOrderDto> ReportAsync(int id, ReportProductionRequest request) => throw new NotSupportedException();

        public Task<WorkOrderDto> CompleteAsync(int id) => throw new NotSupportedException();

        public Task<IEnumerable<WorkOrderDto>> GetTodayWorkOrdersAsync(int? productionLineId = null) => Task.FromResult<IEnumerable<WorkOrderDto>>([]);

        public Task<IEnumerable<WorkOrderDto>> GetDelayedWorkOrdersAsync(DateTime? startDate = null, DateTime? endDate = null) => Task.FromResult<IEnumerable<WorkOrderDto>>([]);
    }

    private sealed class FakeEquipmentService : IEquipmentService
    {
        public Task<IEnumerable<EquipmentDto>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<EquipmentDto>>(
            [
                new EquipmentDto(1, "EQ-001", "CNC 1", null, 1, "Line 1", 100, 1m, true)
            ]);
        }

        public Task<EquipmentDto?> GetByIdAsync(int id) => Task.FromResult<EquipmentDto?>(null);

        public Task<IEnumerable<EquipmentStatusDto>> GetStatusHistoryAsync(int equipmentId) => Task.FromResult<IEnumerable<EquipmentStatusDto>>([]);

        public Task<IEnumerable<EquipmentAlarmDto>> GetAlarmsAsync(int equipmentId) => Task.FromResult<IEnumerable<EquipmentAlarmDto>>([]);

        public Task<OeeDto> CalculateOeeAsync(int equipmentId, DateTime date) => throw new NotSupportedException();
    }
}
