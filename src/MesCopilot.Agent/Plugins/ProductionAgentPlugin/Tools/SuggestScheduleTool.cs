using System.Diagnostics;
using MesCopilot.Agent.Models;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;

namespace MesCopilot.Agent.Plugins.ProductionAgentPlugin.Tools;

public class SuggestScheduleTool
{
    private readonly IWorkOrderService _workOrderService;
    private readonly IEquipmentService _equipmentService;

    public SuggestScheduleTool(
        IWorkOrderService workOrderService,
        IEquipmentService equipmentService)
    {
        _workOrderService = workOrderService;
        _equipmentService = equipmentService;
    }

    public string Name => "SuggestSchedule";

    public string Description => "Suggest a production schedule using earliest-deadline-first ordering and simple equipment load balancing.";

    public async Task<FunctionCallResult> ExecuteAsync(
        int? productionLineId,
        DateTime? startDate,
        DateTime? endDate,
        bool debugMode = false)
    {
        if (productionLineId.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productionLineId.Value, nameof(productionLineId));
        }

        DateTime windowStart = startDate ?? DateTime.UtcNow;
        DateTime windowEnd = endDate ?? windowStart.AddDays(7);
        if (windowStart > windowEnd)
        {
            throw new ArgumentException("Start date must be earlier than or equal to end date.", nameof(startDate));
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        List<WorkOrderDto> orders = (await _workOrderService.GetAllAsync())
            .Where(order =>
                (order.Status == WorkOrderStatus.NotScheduled || order.Status == WorkOrderStatus.Scheduled) &&
                (!productionLineId.HasValue || order.ProductionLineId == productionLineId.Value) &&
                order.PlannedEndTime >= windowStart &&
                order.PlannedEndTime <= windowEnd)
            .OrderBy(order => order.PlannedEndTime)
            .ThenBy(order => order.PlannedStartTime)
            .ToList();

        List<EquipmentDto> equipment = (await _equipmentService.GetAllAsync())
            .Where(item =>
                item.IsActive &&
                (!productionLineId.HasValue || item.ProductionLineId == productionLineId.Value))
            .OrderBy(item => item.Code)
            .ToList();

        if (orders.Count == 0 || equipment.Count == 0)
        {
            stopwatch.Stop();
            return new FunctionCallResult
            {
                Data = new
                {
                    totalOrders = orders.Count,
                    scheduledOrders = Array.Empty<object>(),
                    bottlenecks = Array.Empty<object>()
                },
                Explanation = orders.Count == 0
                    ? "No schedulable work orders found in the selected window."
                    : "No active equipment found for the selected production line.",
                Debug = CreateDebug(debugMode, stopwatch)
            };
        }

        List<ScheduledOrder> scheduledOrders = ApplyEdfScheduling(orders, equipment, windowStart);
        var bottlenecks = BuildBottlenecks(scheduledOrders, windowStart, windowEnd);
        stopwatch.Stop();

        var data = new
        {
            totalOrders = orders.Count,
            scheduledOrders = scheduledOrders.Select(order => new
            {
                workOrderId = order.WorkOrderId,
                workOrderCode = order.WorkOrderCode,
                productName = order.ProductName,
                suggestedStart = order.SuggestedStart,
                suggestedEnd = order.SuggestedEnd,
                priority = order.Priority,
                equipmentCode = order.EquipmentCode
            }).ToList(),
            bottlenecks = bottlenecks.Select(item => new
            {
                equipmentCode = item.EquipmentCode,
                utilizationPercent = item.UtilizationPercent,
                queuedOrdersCount = item.QueuedOrdersCount
            }).ToList(),
            estimatedCompletionDate = scheduledOrders.Max(order => order.SuggestedEnd)
        };

        string bottleneckText = bottlenecks.Count == 0
            ? "No bottleneck equipment identified."
            : $"Bottleneck equipment: {string.Join(", ", bottlenecks.Select(item => item.EquipmentCode))}.";

        return new FunctionCallResult
        {
            Data = data,
            Explanation = $"EDF schedule generated for {orders.Count} work orders. {bottleneckText}",
            Debug = CreateDebug(debugMode, stopwatch)
        };
    }

    private static List<ScheduledOrder> ApplyEdfScheduling(
        IReadOnlyList<WorkOrderDto> orders,
        IReadOnlyList<EquipmentDto> equipment,
        DateTime windowStart)
    {
        Dictionary<int, DateTime> nextAvailableTimeByEquipment = equipment.ToDictionary(
            item => item.Id,
            _ => windowStart);
        List<ScheduledOrder> scheduledOrders = [];

        foreach (WorkOrderDto order in orders)
        {
            EquipmentDto assignedEquipment = equipment
                .OrderBy(item => nextAvailableTimeByEquipment[item.Id])
                .ThenByDescending(item => item.RatedCapacity)
                .First();
            DateTime suggestedStart = nextAvailableTimeByEquipment[assignedEquipment.Id];
            double durationHours = EstimateDurationHours(order, assignedEquipment);
            DateTime suggestedEnd = suggestedStart.AddHours(durationHours);

            scheduledOrders.Add(new ScheduledOrder(
                order.Id,
                order.Code,
                order.ProductName,
                suggestedStart,
                suggestedEnd,
                (order.PlannedEndTime - suggestedEnd).TotalHours < 24 ? "High" : "Normal",
                assignedEquipment.Code));

            nextAvailableTimeByEquipment[assignedEquipment.Id] = suggestedEnd;
        }

        return scheduledOrders;
    }

    private static double EstimateDurationHours(WorkOrderDto order, EquipmentDto equipment)
    {
        int remainingQuantity = Math.Max(1, order.PlannedQuantity - order.CompletedQuantity);
        int hourlyCapacity = Math.Max(1, equipment.RatedCapacity);
        return Math.Max(0.25d, (double)remainingQuantity / hourlyCapacity);
    }

    private static List<Bottleneck> BuildBottlenecks(
        IReadOnlyList<ScheduledOrder> scheduledOrders,
        DateTime windowStart,
        DateTime windowEnd)
    {
        double windowHours = Math.Max(1, (windowEnd - windowStart).TotalHours);

        return scheduledOrders
            .GroupBy(order => order.EquipmentCode)
            .Select(group => new Bottleneck(
                group.Key,
                Math.Round(group.Sum(order => (order.SuggestedEnd - order.SuggestedStart).TotalHours) / windowHours * 100, 1),
                group.Count()))
            .Where(item => item.UtilizationPercent >= 80)
            .OrderByDescending(item => item.UtilizationPercent)
            .ToList();
    }

    private DebugInfo? CreateDebug(bool debugMode, Stopwatch stopwatch)
    {
        return debugMode ? new DebugInfo
        {
            ExecutionTime = $"{stopwatch.ElapsedMilliseconds}ms",
            DataSource = "MesCopilot.Database",
            ToolsCalled = [Name]
        } : null;
    }

    private sealed record ScheduledOrder(
        int WorkOrderId,
        string WorkOrderCode,
        string ProductName,
        DateTime SuggestedStart,
        DateTime SuggestedEnd,
        string Priority,
        string EquipmentCode);

    private sealed record Bottleneck(
        string EquipmentCode,
        double UtilizationPercent,
        int QueuedOrdersCount);
}
