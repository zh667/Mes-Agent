using System.Diagnostics;
using System.Text.Json;
using MesCopilot.Agent.Models;
using MesCopilot.Agent.Plugins.OeeAgentPlugin.Tools;
using MesCopilot.Agent.Plugins.ProductionAgentPlugin.Tools;
using MesCopilot.Agent.Plugins.QualityAgentPlugin.Tools;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Agent;

public class AgentOutputValidationTests
{
    [Fact]
    public async Task ProductionDelayQuestion_ShouldReturnExpectedStructuredFieldsKeywordAndResponseTime()
    {
        var service = new FakeWorkOrderService
        {
            DelayedWorkOrders =
            [
                CreateWorkOrder(1, "WO-DELAY-1"),
                CreateWorkOrder(2, "WO-DELAY-2"),
                CreateWorkOrder(3, "WO-DELAY-3")
            ]
        };
        var tool = new AnalyzeDelayedOrdersTool(service);

        FunctionCallResult result = await ExecuteWithinThreeSecondsAsync(
            () => tool.ExecuteAsync(debugMode: true));

        using JsonDocument data = ToJsonDocument(result.Data);
        AssertHasFields(data, "workOrders", "totalCount");
        Assert.Equal(3, data.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Contains("延期", result.Explanation);
        AssertDebugContract(result, "AnalyzeDelayedOrders");
    }

    [Fact]
    public async Task BatchTraceQuestion_ShouldReturnExpectedStructuredFieldsKeywordAndResponseTime()
    {
        var tool = new TraceBatchTool(new FakeQualityService());

        FunctionCallResult result = await ExecuteWithinThreeSecondsAsync(
            () => tool.ExecuteAsync("B20260708001", debugMode: true));

        using JsonDocument data = ToJsonDocument(result.Data);
        AssertHasFields(data, "workOrder", "processSteps", "equipment");
        Assert.Contains("追溯", result.Explanation);
        AssertDebugContract(result, "TraceBatch");
    }

    [Fact]
    public async Task OeeQuestion_ShouldReturnExpectedStructuredFieldsKeywordAndResponseTime()
    {
        var tool = new CalculateOeeTool(new FakeEquipmentService());

        FunctionCallResult result = await ExecuteWithinThreeSecondsAsync(
            () => tool.ExecuteAsync(102, DateTime.UtcNow.Date, debugMode: true));

        using JsonDocument data = ToJsonDocument(result.Data);
        AssertHasFields(data, "oee", "availability", "performance", "quality");
        Assert.Contains("OEE", result.Explanation);
        AssertDebugContract(result, "CalculateOee");
    }

    private static async Task<FunctionCallResult> ExecuteWithinThreeSecondsAsync(
        Func<Task<FunctionCallResult>> executeAsync)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        FunctionCallResult result = await executeAsync();
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), $"Response took {stopwatch.Elapsed}.");
        return result;
    }

    private static JsonDocument ToJsonDocument(object? data)
    {
        Assert.NotNull(data);
        return JsonDocument.Parse(JsonSerializer.Serialize(data));
    }

    private static void AssertHasFields(JsonDocument data, params string[] fields)
    {
        foreach (string field in fields)
        {
            Assert.True(data.RootElement.TryGetProperty(field, out _), $"Missing expected field: {field}");
        }
    }

    private static void AssertDebugContract(FunctionCallResult result, string toolName)
    {
        Assert.NotNull(result.Debug);
        Assert.Equal(toolName, result.Debug.ToolsCalled?.Single());
        Assert.Equal("MesCopilot.Database", result.Debug.DataSource);
        Assert.Matches(@"^\d+ms$", result.Debug.ExecutionTime);
    }

    private static WorkOrderDto CreateWorkOrder(int id, string code)
    {
        return new WorkOrderDto(
            id,
            code,
            1,
            "Valve Assembly",
            1,
            "Line 1",
            100,
            40,
            38,
            WorkOrderStatus.InProgress,
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(-2),
            null,
            0.4m);
    }

    private sealed class FakeWorkOrderService : IWorkOrderService
    {
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
            return Task.FromResult<IEnumerable<WorkOrderDto>>([]);
        }

        public Task<IEnumerable<WorkOrderDto>> GetDelayedWorkOrdersAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            return Task.FromResult<IEnumerable<WorkOrderDto>>(DelayedWorkOrders);
        }
    }

    private sealed class FakeQualityService : IQualityService
    {
        public Task<IEnumerable<QualityInspectionDto>> GetInspectionsAsync()
        {
            return Task.FromResult<IEnumerable<QualityInspectionDto>>([]);
        }

        public Task<QualityInspectionDto> CreateInspectionAsync(CreateQualityInspectionRequest request)
        {
            throw new NotSupportedException();
        }

        public Task<BatchTraceDto> TraceBatchAsync(string batchNumber)
        {
            var report = new ProductionReportTraceDto(1, batchNumber, 10, 20, 102, "op-1", "Operator", DateTime.UtcNow, 100, 98);
            var inspection = new QualityInspectionDto(1, "QI-TRACE", batchNumber, 10, 20, "qc-1", "QC One", 100, 98, 2, InspectionStatus.Fail, DateTime.UtcNow, "Scratch");
            return Task.FromResult(new BatchTraceDto(batchNumber, [report], [inspection]));
        }

        public Task<IEnumerable<DefectAnalysisDto>> AnalyzeDefectsAsync()
        {
            return Task.FromResult<IEnumerable<DefectAnalysisDto>>([]);
        }
    }

    private sealed class FakeEquipmentService : IEquipmentService
    {
        public Task<IEnumerable<EquipmentDto>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<EquipmentDto>>([]);
        }

        public Task<EquipmentDto?> GetByIdAsync(int id)
        {
            return Task.FromResult<EquipmentDto?>(null);
        }

        public Task<IEnumerable<EquipmentStatusDto>> GetStatusHistoryAsync(int equipmentId)
        {
            return Task.FromResult<IEnumerable<EquipmentStatusDto>>([]);
        }

        public Task<IEnumerable<EquipmentAlarmDto>> GetAlarmsAsync(int equipmentId)
        {
            return Task.FromResult<IEnumerable<EquipmentAlarmDto>>([]);
        }

        public Task<OeeDto> CalculateOeeAsync(int equipmentId, DateTime date)
        {
            return Task.FromResult(new OeeDto(equipmentId, date, 0.9m, 0.82m, 0.98m, 0.72m, 120, 118, 480));
        }
    }
}
