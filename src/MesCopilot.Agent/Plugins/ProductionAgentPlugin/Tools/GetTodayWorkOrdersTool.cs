using System.Diagnostics;
using MesCopilot.Agent.Models;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;

namespace MesCopilot.Agent.Plugins.ProductionAgentPlugin.Tools;

public class GetTodayWorkOrdersTool
{
    private readonly IWorkOrderService _workOrderService;

    public GetTodayWorkOrdersTool(IWorkOrderService workOrderService)
    {
        _workOrderService = workOrderService;
    }

    public string Name => "GetTodayWorkOrders";

    public string Description => "Query today's work orders with optional production line filtering.";

    public async Task<FunctionCallResult> ExecuteAsync(int? productionLineId = null, bool debugMode = false)
    {
        var stopwatch = Stopwatch.StartNew();
        var workOrders = (await _workOrderService.GetTodayWorkOrdersAsync(productionLineId)).ToList();
        stopwatch.Stop();

        var data = new
        {
            workOrders = workOrders.Select(workOrder => new
            {
                workOrder.Id,
                workOrder.Code,
                workOrder.ProductName,
                workOrder.Status,
                workOrder.PlannedQuantity,
                workOrder.CompletedQuantity,
                workOrder.Progress
            }),
            totalCount = workOrders.Count,
            inProgressCount = workOrders.Count(workOrder => workOrder.Status == WorkOrderStatus.InProgress),
            completedCount = workOrders.Count(workOrder => workOrder.Status == WorkOrderStatus.Completed)
        };

        return new FunctionCallResult
        {
            Data = data,
            Explanation = $"Today has {data.totalCount} work orders: {data.inProgressCount} in progress and {data.completedCount} completed.",
            Debug = debugMode ? new DebugInfo
            {
                SqlExecuted = "SELECT * FROM WorkOrders WHERE CreatedAt >= @today AND CreatedAt < @tomorrow",
                ExecutionTime = $"{stopwatch.ElapsedMilliseconds}ms",
                DataSource = "MesCopilot.Database",
                ToolsCalled = new List<string> { Name }
            } : null
        };
    }
}
