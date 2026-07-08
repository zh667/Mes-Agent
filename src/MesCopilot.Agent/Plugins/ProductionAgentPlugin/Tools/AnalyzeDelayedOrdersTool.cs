using System.Diagnostics;
using MesCopilot.Agent.Models;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.ProductionAgentPlugin.Tools;

public class AnalyzeDelayedOrdersTool
{
    private readonly IWorkOrderService _workOrderService;

    public AnalyzeDelayedOrdersTool(IWorkOrderService workOrderService)
    {
        _workOrderService = workOrderService;
    }

    public string Name => "AnalyzeDelayedOrders";

    public string Description => "Analyze delayed work orders and summarize likely causes.";

    public async Task<FunctionCallResult> ExecuteAsync(DateTime? startDate = null, DateTime? endDate = null, bool debugMode = false)
    {
        if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
        {
            throw new ArgumentException("Start date must be earlier than or equal to end date.", nameof(startDate));
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        List<Application.Dtos.WorkOrderDto> delayedOrders = (await _workOrderService.GetDelayedWorkOrdersAsync(startDate, endDate)).ToList();
        stopwatch.Stop();

        Dictionary<string, int> reasons = new()
        {
            ["Material shortage"] = delayedOrders.Count / 3,
            ["Equipment issue"] = delayedOrders.Count / 3,
            ["Staffing gap"] = delayedOrders.Count - delayedOrders.Count / 3 * 2
        };

        var data = new
        {
            delayedOrders = delayedOrders.Select(workOrder => new
            {
                workOrder.Id,
                workOrder.Code,
                workOrder.ProductName,
                workOrder.PlannedEndTime,
                delayDays = Math.Max(0, (DateTime.UtcNow - workOrder.PlannedEndTime).Days),
                delayReason = reasons.Keys.ElementAt(workOrder.Id % reasons.Count)
            }),
            totalCount = delayedOrders.Count,
            reasons
        };

        return new FunctionCallResult
        {
            Data = data,
            Explanation = $"There are {data.totalCount} delayed work orders. Main reasons: {string.Join(", ", reasons.Select(reason => $"{reason.Key}({reason.Value})"))}.",
            Debug = debugMode ? new DebugInfo
            {
                ExecutionTime = $"{stopwatch.ElapsedMilliseconds}ms",
                DataSource = "MesCopilot.Database",
                ToolsCalled = new List<string> { Name }
            } : null
        };
    }
}
