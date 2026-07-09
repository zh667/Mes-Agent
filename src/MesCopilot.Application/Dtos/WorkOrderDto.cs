using MesCopilot.Domain.Enums;

namespace MesCopilot.Application.Dtos;

public record WorkOrderDto(
    int Id,
    string Code,
    int ProductId,
    string ProductName,
    int ProductionLineId,
    string ProductionLineName,
    int PlannedQuantity,
    int CompletedQuantity,
    int QualifiedQuantity,
    WorkOrderStatus Status,
    DateTime PlannedStartTime,
    DateTime PlannedEndTime,
    DateTime? ActualStartTime,
    DateTime? ActualEndTime,
    decimal Progress
);

public record CreateWorkOrderRequest(
    string Code,
    int ProductId,
    int ProductionLineId,
    int PlannedQuantity,
    DateTime PlannedStartTime,
    DateTime PlannedEndTime
);

public record ReportProductionRequest(
    int Quantity,
    int QualifiedQuantity,
    string OperatorId,
    string OperatorName
);
