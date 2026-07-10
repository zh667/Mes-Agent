using MesCopilot.Domain.Enums;

namespace MesCopilot.Application.Dtos.Reports;

public record ProductionReportDto(
    DateTime ReportDate,
    int? ProductionLineId,
    string ProductionLineName,
    int TotalOrders,
    int CompletedOrders,
    int DelayedOrders,
    decimal CompletionRate,
    int TotalPlannedQuantity,
    int TotalActualQuantity,
    decimal OutputRate,
    IReadOnlyList<ProductionReportLineItemDto> LineItems);

public record ProductionReportLineItemDto(
    string WorkOrderCode,
    string ProductName,
    int PlannedQuantity,
    int ActualQuantity,
    WorkOrderStatus Status,
    DateTime DueDate,
    bool IsDelayed);
