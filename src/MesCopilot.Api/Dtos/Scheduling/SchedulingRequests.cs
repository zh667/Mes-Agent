using System.ComponentModel.DataAnnotations;

namespace MesCopilot.Api.Dtos.Scheduling;

public sealed class GenerateScheduleRequest
{
    [Required, MinLength(1), MaxLength(50)]
    public IReadOnlyList<int> WorkOrderIds { get; init; } = [];
    public DateTime ScheduleStartUtc { get; init; }
}

public sealed class AdjustOperationRequest
{
    [Range(1, int.MaxValue)]
    public int EquipmentId { get; init; }
    public DateTime PlannedStartUtc { get; init; }
    public DateTime PlannedEndUtc { get; init; }
    public uint Version { get; init; }
}
