namespace MesCopilot.Application.Dtos.Reports;

public record OeeReportDto(
    DateTime FromDate,
    DateTime ToDate,
    IReadOnlyList<int> EquipmentIds,
    decimal AverageOee,
    decimal AverageAvailability,
    decimal AveragePerformance,
    decimal AverageQuality,
    IReadOnlyList<OeeReportLineItemDto> LineItems);

public record OeeReportLineItemDto(
    int EquipmentId,
    string EquipmentName,
    DateTime Date,
    decimal Availability,
    decimal Performance,
    decimal Quality,
    decimal Oee,
    int RunningMinutes,
    int TotalOutput,
    int QualifiedOutput);
