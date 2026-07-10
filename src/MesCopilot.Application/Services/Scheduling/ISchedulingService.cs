using MesCopilot.Application.Dtos.Scheduling;

namespace MesCopilot.Application.Services.Scheduling;

public interface ISchedulingService
{
    Task<ScheduleGenerationResultDto> GenerateAsync(
        IReadOnlyList<int> workOrderIds,
        DateTime scheduleStartUtc,
        CancellationToken cancellationToken = default);

    Task<GanttScheduleDto> GetGanttAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task<ScheduledWorkOrderDto?> GetWorkOrderAsync(int workOrderId, CancellationToken cancellationToken = default);

    Task<ScheduledOperationDto> AdjustOperationAsync(
        int operationId,
        int equipmentId,
        DateTime plannedStartUtc,
        DateTime plannedEndUtc,
        uint version,
        CancellationToken cancellationToken = default);
}

public sealed class ScheduleValidationException : Exception
{
    public ScheduleValidationException(string message) : base(message) { }
}

public sealed class ScheduleConflictException : Exception
{
    public ScheduleConflictException(string message) : base(message) { }
}
