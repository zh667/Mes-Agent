using MesCopilot.Domain.Enums;

namespace MesCopilot.Application.Dtos.Reports;

public record QualityReportDto(
    DateTime FromDate,
    DateTime ToDate,
    int TotalInspections,
    int PassedInspections,
    int FailedInspections,
    decimal PassRate,
    int TotalDefects,
    IReadOnlyList<QualityReportLineItemDto> LineItems,
    IReadOnlyList<DefectSummaryItemDto> DefectSummary);

public record QualityReportLineItemDto(
    string InspectionCode,
    string BatchNumber,
    int WorkOrderId,
    string InspectorName,
    DateTime InspectionDate,
    InspectionStatus Status,
    int DefectCount);

public record DefectSummaryItemDto(
    string DefectType,
    int Count,
    decimal Percentage);
