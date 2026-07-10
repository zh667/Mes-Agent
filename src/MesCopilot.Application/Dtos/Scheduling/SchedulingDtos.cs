using MesCopilot.Domain.Enums;

namespace MesCopilot.Application.Dtos.Scheduling;

public sealed record ScheduledOperationDto(
    int Id,
    int WorkOrderId,
    string WorkOrderCode,
    int ProcessStepId,
    string ProcessStepName,
    int Sequence,
    int EquipmentId,
    string EquipmentCode,
    DateTime PlannedStartTime,
    DateTime PlannedEndTime,
    uint Version);

public sealed record ScheduleGenerationResultDto(
    IReadOnlyList<int> WorkOrderIds,
    IReadOnlyList<ScheduledOperationDto> Operations);

public sealed record GanttEquipmentRowDto(
    int EquipmentId,
    string EquipmentCode,
    string EquipmentName,
    IReadOnlyList<ScheduledOperationDto> Operations);

public sealed record GanttScheduleDto(
    DateTime From,
    DateTime To,
    IReadOnlyList<GanttEquipmentRowDto> EquipmentRows);

public sealed record ScheduledWorkOrderDto(
    int WorkOrderId,
    string Code,
    WorkOrderStatus Status,
    IReadOnlyList<ScheduledOperationDto> Operations);
