using System.Data;
using MesCopilot.Application.Dtos.Scheduling;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using EquipmentEntity = MesCopilot.Domain.Entities.Equipment.Equipment;

namespace MesCopilot.Application.Services.Scheduling;

public sealed class SequentialSchedulingService : ISchedulingService
{
    private const int MaxWorkOrders = 50;
    private static readonly TimeSpan MaxGanttRange = TimeSpan.FromDays(31);
    private readonly MesDbContext _context;

    public SequentialSchedulingService(MesDbContext context)
    {
        _context = context;
    }

    public async Task<ScheduleGenerationResultDto> GenerateAsync(
        IReadOnlyList<int> workOrderIds,
        DateTime scheduleStartUtc,
        CancellationToken cancellationToken = default)
    {
        List<int> ids = workOrderIds.Distinct().ToList();
        if (ids.Count is 0 or > MaxWorkOrders)
        {
            throw new ArgumentOutOfRangeException(nameof(workOrderIds), $"Between 1 and {MaxWorkOrders} work orders are required.");
        }

        DateTime startUtc = EnsureUtc(scheduleStartUtc);
        IDbContextTransaction? transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            List<WorkOrder> orders = await _context.WorkOrders
                .Where(order => ids.Contains(order.Id))
                .OrderBy(order => order.PlannedStartTime)
                .ThenBy(order => order.Id)
                .ToListAsync(cancellationToken);
            if (orders.Count != ids.Count)
            {
                throw new ScheduleValidationException("One or more work orders were not found.");
            }

            if (orders.Any(order => order.Status is not WorkOrderStatus.NotScheduled and not WorkOrderStatus.Scheduled))
            {
                throw new ScheduleValidationException("Only NotScheduled or Scheduled work orders can be generated.");
            }

            HashSet<int> productIds = orders.Select(order => order.ProductId).ToHashSet();
            List<ProcessRoute> routes = await _context.ProcessRoutes
                .AsNoTracking()
                .Include(route => route.ProcessSteps)
                .Where(route => route.IsActive && productIds.Contains(route.ProductId))
                .ToListAsync(cancellationToken);
            Dictionary<int, ProcessRoute> routeByProduct = routes
                .GroupBy(route => route.ProductId)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(route => route.CreatedAt).First());
            if (orders.Any(order => !routeByProduct.ContainsKey(order.ProductId)))
            {
                throw new ScheduleValidationException("An active process route is required for every work order.");
            }

            HashSet<int> workstationIds = routes.SelectMany(route => route.ProcessSteps)
                .Where(step => step.WorkstationId.HasValue)
                .Select(step => step.WorkstationId!.Value)
                .ToHashSet();
            List<EquipmentEntity> equipment = await _context.Equipment
                .AsNoTracking()
                .Where(item => item.IsActive && item.WorkstationId.HasValue && workstationIds.Contains(item.WorkstationId.Value))
                .OrderBy(item => item.Id)
                .ToListAsync(cancellationToken);
            Dictionary<int, List<EquipmentEntity>> equipmentByStation = equipment
                .GroupBy(item => item.WorkstationId!.Value)
                .ToDictionary(group => group.Key, group => group.ToList());

            List<WorkOrderOperation> replaceable = await _context.WorkOrderOperations
                .Where(operation => ids.Contains(operation.WorkOrderId) && operation.ActualStartTime == null)
                .ToListAsync(cancellationToken);
            _context.WorkOrderOperations.RemoveRange(replaceable);
            HashSet<int> replaceableIds = replaceable.Select(operation => operation.Id).ToHashSet();
            List<WorkOrderOperation> occupied = await _context.WorkOrderOperations
                .AsNoTracking()
                .Where(operation => operation.EquipmentId.HasValue && !replaceableIds.Contains(operation.Id))
                .ToListAsync(cancellationToken);
            Dictionary<int, List<(DateTime Start, DateTime End)>> occupancy = occupied
                .GroupBy(operation => operation.EquipmentId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(operation => (operation.PlannedStartTime, operation.PlannedEndTime)).OrderBy(slot => slot.PlannedStartTime).ToList());

            List<WorkOrderOperation> created = [];
            foreach (WorkOrder order in orders)
            {
                ProcessRoute route = routeByProduct[order.ProductId];
                DateTime cursor = Max(startUtc, EnsureUtc(order.PlannedStartTime));
                foreach (ProcessStep step in route.ProcessSteps.OrderBy(step => step.Sequence).ThenBy(step => step.Id))
                {
                    if (!step.WorkstationId.HasValue ||
                        !equipmentByStation.TryGetValue(step.WorkstationId.Value, out List<EquipmentEntity>? candidates) ||
                        candidates.Count == 0)
                    {
                        throw new ScheduleValidationException($"No active equipment is available for process step {step.Code}.");
                    }

                    TimeSpan duration = TimeSpan.FromMinutes(Math.Max(1m, step.StandardTime * order.PlannedQuantity) is decimal minutes ? (double)minutes : 1d);
                    (EquipmentEntity Equipment, DateTime Start) selection = candidates
                        .Select(candidate => (Equipment: candidate, Start: FindEarliest(cursor, duration, occupancy.GetValueOrDefault(candidate.Id))))
                        .OrderBy(candidate => candidate.Start)
                        .ThenBy(candidate => candidate.Equipment.Id)
                        .First();
                    DateTime end = selection.Start.Add(duration);
                    WorkOrderOperation operation = new()
                    {
                        WorkOrderId = order.Id,
                        ProcessStepId = step.Id,
                        EquipmentId = selection.Equipment.Id,
                        Sequence = step.Sequence,
                        PlannedStartTime = selection.Start,
                        PlannedEndTime = end
                    };
                    created.Add(operation);
                    occupancy.GetOrAdd(selection.Equipment.Id).Add((selection.Start, end));
                    occupancy[selection.Equipment.Id].Sort((left, right) => left.Start.CompareTo(right.Start));
                    cursor = end;
                }

                order.Status = WorkOrderStatus.Scheduled;
                order.PlannedStartTime = created.Where(operation => operation.WorkOrderId == order.Id).Min(operation => operation.PlannedStartTime);
                order.PlannedEndTime = cursor;
                order.UpdatedAt = DateTime.UtcNow;
            }

            _context.WorkOrderOperations.AddRange(created);
            await _context.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);

            return new ScheduleGenerationResultDto(ids, await MapOperationsAsync(created.Select(item => item.Id), cancellationToken));
        }
        catch (DbUpdateException exception) when (exception is DbUpdateConcurrencyException || _context.Database.IsRelational())
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw new ScheduleConflictException("The schedule changed concurrently. Reload and try again.");
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    public async Task<GanttScheduleDto> GetGanttAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        DateTime from = EnsureUtc(fromUtc);
        DateTime to = EnsureUtc(toUtc);
        if (from >= to) throw new ArgumentException("The Gantt range is invalid.");
        if (to - from > MaxGanttRange)
        {
            throw new ArgumentOutOfRangeException(nameof(toUtc), "The Gantt range cannot exceed 31 days.");
        }
        List<ScheduledOperationDto> operations = await MapOperationsAsync(
            _context.WorkOrderOperations.AsNoTracking()
                .Where(operation => operation.EquipmentId.HasValue && operation.PlannedStartTime < to && operation.PlannedEndTime > from)
                .Select(operation => operation.Id),
            cancellationToken);
        List<GanttEquipmentRowDto> rows = operations
            .GroupBy(operation => new { operation.EquipmentId, operation.EquipmentCode })
            .Select(group => new GanttEquipmentRowDto(group.Key.EquipmentId, group.Key.EquipmentCode, group.Key.EquipmentCode, group.OrderBy(item => item.PlannedStartTime).ToList()))
            .OrderBy(row => row.EquipmentCode)
            .ToList();
        return new GanttScheduleDto(from, to, rows);
    }

    public async Task<ScheduledWorkOrderDto?> GetWorkOrderAsync(int workOrderId, CancellationToken cancellationToken = default)
    {
        WorkOrder? order = await _context.WorkOrders.AsNoTracking().FirstOrDefaultAsync(item => item.Id == workOrderId, cancellationToken);
        if (order is null) return null;
        List<ScheduledOperationDto> operations = await MapOperationsAsync(
            _context.WorkOrderOperations.Where(item => item.WorkOrderId == workOrderId).Select(item => item.Id),
            cancellationToken);
        return new ScheduledWorkOrderDto(order.Id, order.Code, order.Status, operations);
    }

    public async Task<ScheduledOperationDto> AdjustOperationAsync(
        int operationId, int equipmentId, DateTime plannedStartUtc, DateTime plannedEndUtc, uint version,
        CancellationToken cancellationToken = default)
    {
        DateTime start = EnsureUtc(plannedStartUtc);
        DateTime end = EnsureUtc(plannedEndUtc);
        if (start >= end) throw new ScheduleValidationException("Planned end must be after planned start.");
        IDbContextTransaction? transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            if (transaction is not null)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtext({_context.CurrentTenantId!}), {equipmentId})",
                    cancellationToken);
            }

            WorkOrderOperation operation = await _context.WorkOrderOperations
                .Include(item => item.WorkOrder)
                .FirstOrDefaultAsync(item => item.Id == operationId, cancellationToken)
                ?? throw new ScheduleValidationException("Operation was not found.");
            if (operation.WorkOrder.Status != WorkOrderStatus.Scheduled)
            {
                throw new ScheduleValidationException("Only operations on scheduled work orders can be adjusted.");
            }
            if (operation.Version != version) throw new ScheduleConflictException("The operation has changed.");

            bool equipmentAvailable = await _context.Equipment.AsNoTracking()
                .AnyAsync(item => item.Id == equipmentId && item.IsActive, cancellationToken);
            if (!equipmentAvailable)
            {
                throw new ScheduleValidationException("The selected equipment is not available in the active tenant.");
            }

            bool overlaps = await _context.WorkOrderOperations.AsNoTracking().AnyAsync(
                item => item.Id != operationId && item.EquipmentId == equipmentId && item.PlannedStartTime < end && item.PlannedEndTime > start,
                cancellationToken);
            if (overlaps) throw new ScheduleConflictException("The selected equipment is already occupied in that interval.");
            operation.EquipmentId = equipmentId;
            operation.PlannedStartTime = start;
            operation.PlannedEndTime = end;
            await _context.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return (await MapOperationsAsync([operation.Id], cancellationToken)).Single();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw new ScheduleConflictException("The schedule changed concurrently. Reload and try again.");
        }
        catch (Exception exception) when (IsSerializationFailure(exception))
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw new ScheduleConflictException("The schedule changed concurrently. Reload and try again.");
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    private async Task<List<ScheduledOperationDto>> MapOperationsAsync(IEnumerable<int> operationIds, CancellationToken cancellationToken)
    {
        List<int> ids = operationIds.ToList();
        return await _context.WorkOrderOperations.AsNoTracking()
            .Where(operation => ids.Contains(operation.Id))
            .OrderBy(operation => operation.PlannedStartTime)
            .Select(operation => new ScheduledOperationDto(
                operation.Id, operation.WorkOrderId, operation.WorkOrder.Code, operation.ProcessStepId,
                operation.ProcessStep.Name, operation.Sequence, operation.EquipmentId!.Value,
                operation.Equipment!.Code, operation.PlannedStartTime, operation.PlannedEndTime, operation.Version))
            .ToListAsync(cancellationToken);
    }

    private static DateTime FindEarliest(DateTime earliest, TimeSpan duration, List<(DateTime Start, DateTime End)>? slots)
    {
        DateTime candidate = earliest;
        foreach ((DateTime start, DateTime end) in slots ?? [])
        {
            if (candidate.Add(duration) <= start) break;
            if (candidate < end && candidate.Add(duration) > start) candidate = end;
        }
        return candidate;
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static DateTime Max(DateTime left, DateTime right) => left >= right ? left : right;

    private static bool IsSerializationFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.SerializationFailure)
            {
                return true;
            }
        }

        return false;
    }
}

file static class SchedulingDictionaryExtensions
{
    public static List<(DateTime Start, DateTime End)> GetOrAdd(
        this Dictionary<int, List<(DateTime Start, DateTime End)>> dictionary,
        int key)
    {
        if (!dictionary.TryGetValue(key, out List<(DateTime Start, DateTime End)>? value))
        {
            value = [];
            dictionary[key] = value;
        }
        return value;
    }
}
