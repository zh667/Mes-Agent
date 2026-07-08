using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using EquipmentEntity = MesCopilot.Domain.Entities.Equipment.Equipment;

namespace MesCopilot.Application.Services;

public class EquipmentService : IEquipmentService
{
    private readonly MesDbContext _context;

    public EquipmentService(MesDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EquipmentDto>> GetAllAsync()
    {
        var equipment = await BaseQuery().ToListAsync();
        return equipment.Select(ToDto);
    }

    public async Task<EquipmentDto?> GetByIdAsync(int id)
    {
        var equipment = await BaseQuery().FirstOrDefaultAsync(item => item.Id == id);
        return equipment is null ? null : ToDto(equipment);
    }

    public async Task<IEnumerable<EquipmentStatusDto>> GetStatusHistoryAsync(int equipmentId)
    {
        var statuses = await _context.EquipmentStatuses
            .AsNoTracking()
            .Where(status => status.EquipmentId == equipmentId)
            .OrderByDescending(status => status.StartTime)
            .ToListAsync();

        return statuses.Select(ToDto);
    }

    public async Task<IEnumerable<EquipmentAlarmDto>> GetAlarmsAsync(int equipmentId)
    {
        var alarms = await _context.EquipmentAlarms
            .AsNoTracking()
            .Where(alarm => alarm.EquipmentId == equipmentId)
            .OrderByDescending(alarm => alarm.OccurredAt)
            .ToListAsync();

        return alarms.Select(ToDto);
    }

    public async Task<OeeDto> CalculateOeeAsync(int equipmentId, DateTime date)
    {
        var day = date.Date;
        var nextDay = day.AddDays(1);
        var equipment = await _context.Equipment.AsNoTracking().FirstOrDefaultAsync(item => item.Id == equipmentId)
            ?? throw new InvalidOperationException($"Equipment {equipmentId} not found.");

        var statuses = await _context.EquipmentStatuses
            .AsNoTracking()
            .Where(status =>
                status.EquipmentId == equipmentId &&
                status.StartTime >= day &&
                status.StartTime < nextDay)
            .ToListAsync();

        var reports = await _context.ProductionReports
            .AsNoTracking()
            .Where(report =>
                report.EquipmentId == equipmentId &&
                report.Timestamp >= day &&
                report.Timestamp < nextDay)
            .ToListAsync();

        var runningMinutes = statuses
            .Where(status => status.State == EquipmentState.Running)
            .Sum(GetDurationMinutes);
        var totalOutput = reports.Sum(report => report.Quantity);
        var qualifiedOutput = reports.Sum(report => report.QualifiedQuantity);

        var plannedMinutes = 24m * 60m;
        var availability = ClampRatio(runningMinutes / plannedMinutes);
        var performance = runningMinutes > 0
            ? ClampRatio((totalOutput * equipment.IdealCycleTime) / (runningMinutes * 60m))
            : 0m;
        var quality = totalOutput > 0
            ? ClampRatio((decimal)qualifiedOutput / totalOutput)
            : 0m;
        var oee = ClampRatio(availability * performance * quality);

        return new OeeDto(equipmentId, day, availability, performance, quality, oee, totalOutput, qualifiedOutput, runningMinutes);
    }

    private IQueryable<EquipmentEntity> BaseQuery()
    {
        return _context.Equipment
            .AsNoTracking()
            .Include(equipment => equipment.ProductionLine);
    }

    private static int GetDurationMinutes(EquipmentStatus status)
    {
        if (status.DurationMinutes.HasValue)
        {
            return status.DurationMinutes.Value;
        }

        return status.EndTime.HasValue
            ? Math.Max(0, (int)(status.EndTime.Value - status.StartTime).TotalMinutes)
            : 0;
    }

    private static decimal ClampRatio(decimal value)
    {
        return Math.Min(1m, Math.Max(0m, value));
    }

    private static EquipmentDto ToDto(EquipmentEntity equipment)
    {
        return new EquipmentDto(
            equipment.Id,
            equipment.Code,
            equipment.Name,
            equipment.Model,
            equipment.ProductionLineId,
            equipment.ProductionLine?.Name,
            equipment.RatedCapacity,
            equipment.IdealCycleTime,
            equipment.IsActive);
    }

    private static EquipmentStatusDto ToDto(EquipmentStatus status)
    {
        return new EquipmentStatusDto(
            status.Id,
            status.EquipmentId,
            status.State,
            status.StartTime,
            status.EndTime,
            status.DurationMinutes,
            status.Remarks);
    }

    private static EquipmentAlarmDto ToDto(EquipmentAlarm alarm)
    {
        return new EquipmentAlarmDto(
            alarm.Id,
            alarm.EquipmentId,
            alarm.AlarmCode,
            alarm.Message,
            alarm.Level,
            alarm.OccurredAt,
            alarm.AcknowledgedAt,
            alarm.ResolvedAt,
            alarm.HandlerName,
            alarm.Resolution);
    }
}
