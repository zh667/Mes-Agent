using MesCopilot.Domain.Enums;

namespace MesCopilot.Application.Dtos;

public record EquipmentDto(
    int Id,
    string Code,
    string Name,
    string? Model,
    int? ProductionLineId,
    string? ProductionLineName,
    int RatedCapacity,
    decimal IdealCycleTime,
    bool IsActive
);

public record EquipmentStatusDto(
    int Id,
    int EquipmentId,
    EquipmentState State,
    DateTime StartTime,
    DateTime? EndTime,
    int? DurationMinutes,
    string? Remarks
);

public record EquipmentAlarmDto(
    int Id,
    int EquipmentId,
    string AlarmCode,
    string Message,
    int Level,
    DateTime OccurredAt,
    DateTime? AcknowledgedAt,
    DateTime? ResolvedAt,
    string? HandlerName,
    string? Resolution
);

public record OeeDto(
    int EquipmentId,
    DateTime Date,
    decimal Availability,
    decimal Performance,
    decimal Quality,
    decimal Oee,
    int TotalOutput,
    int QualifiedOutput,
    int RunningMinutes
);
