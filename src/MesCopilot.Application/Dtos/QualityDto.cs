using MesCopilot.Domain.Enums;

namespace MesCopilot.Application.Dtos;

public record QualityInspectionDto(
    int Id,
    string Code,
    string BatchNumber,
    int WorkOrderId,
    int ProcessStepId,
    string InspectorId,
    string InspectorName,
    int InspectedQuantity,
    int PassedQuantity,
    int FailedQuantity,
    InspectionStatus Status,
    DateTime InspectionTime,
    string? Remarks
);

public record CreateQualityInspectionRequest(
    string Code,
    string BatchNumber,
    int WorkOrderId,
    int ProcessStepId,
    string InspectorId,
    string InspectorName,
    int InspectedQuantity,
    int PassedQuantity,
    int FailedQuantity,
    InspectionStatus Status,
    string? Remarks
);

public record ProductionReportTraceDto(
    int Id,
    string BatchNumber,
    int WorkOrderId,
    int ProcessStepId,
    int EquipmentId,
    string OperatorId,
    string OperatorName,
    DateTime Timestamp,
    int Quantity,
    int QualifiedQuantity
);

public record BatchTraceDto(
    string BatchNumber,
    IReadOnlyList<ProductionReportTraceDto> ProductionReports,
    IReadOnlyList<QualityInspectionDto> Inspections
);

public record DefectAnalysisDto(
    int DefectTypeId,
    string DefectTypeCode,
    string DefectTypeName,
    int TotalQuantity
);
