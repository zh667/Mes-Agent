namespace MesCopilot.Application.Dtos.Realtime;

public record EquipmentStatusUpdate(
    int EquipmentId,
    string EquipmentCode,
    int? ProductionLineId,
    string State,
    DateTime Timestamp
);
