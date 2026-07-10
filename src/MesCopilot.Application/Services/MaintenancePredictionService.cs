using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Application.Services;

public class MaintenancePredictionService : IMaintenancePredictionService
{
    private readonly MesDbContext _context;

    public MaintenancePredictionService(MesDbContext context)
    {
        _context = context;
    }

    public async Task<MaintenancePredictionDto> PredictMaintenanceAsync(int equipmentId)
    {
        var equipment = await _context.Equipment
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == equipmentId)
            ?? throw new InvalidOperationException($"Equipment {equipmentId} not found.");

        DateTime now = DateTime.UtcNow;
        DateTime since = now.AddDays(-30);
        List<DowntimeRecord> downtimeRecords = await _context.DowntimeRecords
            .AsNoTracking()
            .Where(record => record.EquipmentId == equipmentId && record.StartTime >= since)
            .OrderBy(record => record.StartTime)
            .ToListAsync();

        int alarmCount = await _context.EquipmentAlarms
            .AsNoTracking()
            .CountAsync(alarm => alarm.EquipmentId == equipmentId && alarm.OccurredAt >= since);

        double mtbfHours = CalculateMtbfHours(downtimeRecords, now);
        double mttrHours = CalculateMttrHours(downtimeRecords);
        double healthScore = CalculateHealthScore(mtbfHours, mttrHours, downtimeRecords.Count, alarmCount);
        TrendDto trend = CalculateTrend(downtimeRecords, now);

        return new MaintenancePredictionDto
        {
            EquipmentId = equipment.Id,
            EquipmentCode = equipment.Code,
            EquipmentName = equipment.Name,
            HealthScore = Math.Round(healthScore, 1),
            HealthLevel = ToHealthLevel(healthScore),
            MtbfHours = Math.Round(mtbfHours, 1),
            MttrHours = Math.Round(mttrHours, 1),
            PredictedNextFailureAt = PredictNextFailure(downtimeRecords, mtbfHours, now),
            MaintenanceRecommendation = BuildRecommendation(healthScore, mtbfHours, trend),
            RecentFailures = downtimeRecords
                .OrderByDescending(record => record.StartTime)
                .Take(5)
                .Select(record => new RecentFailureDto(
                    record.StartTime,
                    record.Reason,
                    GetDurationMinutes(record)))
                .ToList(),
            Trend = trend
        };
    }

    private static double CalculateMtbfHours(IReadOnlyList<DowntimeRecord> records, DateTime now)
    {
        if (records.Count == 0)
        {
            return 720;
        }

        if (records.Count == 1)
        {
            return Math.Max(1, (now - records[0].StartTime).TotalHours);
        }

        List<double> intervals = [];
        for (int index = 1; index < records.Count; index++)
        {
            intervals.Add(Math.Max(1, (records[index].StartTime - records[index - 1].StartTime).TotalHours));
        }

        return intervals.Average();
    }

    private static double CalculateMttrHours(IReadOnlyList<DowntimeRecord> records)
    {
        List<double> repairHours = records
            .Select(GetDurationMinutes)
            .Where(minutes => minutes > 0)
            .Select(minutes => minutes / 60d)
            .ToList();

        return repairHours.Count == 0 ? 0 : repairHours.Average();
    }

    private static double GetDurationMinutes(DowntimeRecord record)
    {
        if (record.DurationMinutes.HasValue)
        {
            return record.DurationMinutes.Value;
        }

        return record.EndTime.HasValue
            ? Math.Max(0, (record.EndTime.Value - record.StartTime).TotalMinutes)
            : 0;
    }

    private static double CalculateHealthScore(double mtbfHours, double mttrHours, int downtimeCount, int alarmCount)
    {
        double score = 100;

        if (mtbfHours < 24)
        {
            score -= 30;
        }
        else if (mtbfHours < 72)
        {
            score -= 18;
        }
        else if (mtbfHours < 168)
        {
            score -= 8;
        }

        if (mttrHours > 8)
        {
            score -= 20;
        }
        else if (mttrHours > 4)
        {
            score -= 12;
        }
        else if (mttrHours > 1)
        {
            score -= 5;
        }

        score -= Math.Min(downtimeCount * 7, 35);
        score -= Math.Min(alarmCount * 2, 20);

        return Math.Clamp(score, 0, 100);
    }

    private static string ToHealthLevel(double healthScore)
    {
        if (healthScore >= 80)
        {
            return "Healthy";
        }

        if (healthScore >= 60)
        {
            return "Watch";
        }

        if (healthScore >= 40)
        {
            return "Warning";
        }

        return "Critical";
    }

    private static DateTime? PredictNextFailure(IReadOnlyList<DowntimeRecord> records, double mtbfHours, DateTime now)
    {
        if (records.Count < 2)
        {
            return null;
        }

        DateTime predicted = records[^1].StartTime.AddHours(mtbfHours);
        return predicted <= now ? now.AddHours(Math.Max(1, mtbfHours * 0.1)) : predicted;
    }

    private static TrendDto CalculateTrend(IReadOnlyList<DowntimeRecord> records, DateTime now)
    {
        if (records.Count < 4)
        {
            return new TrendDto();
        }

        DateTime midpoint = now.AddDays(-15);
        int recent = records.Count(record => record.StartTime >= midpoint);
        int older = records.Count - recent;
        double changePercent = older == 0 ? 100 : ((double)recent - older) / older * 100;

        if (changePercent > 25)
        {
            return new TrendDto
            {
                Direction = "Deteriorating",
                ChangePercent = Math.Round(changePercent, 1),
                Description = "Failure frequency is increasing."
            };
        }

        if (changePercent < -25)
        {
            return new TrendDto
            {
                Direction = "Improving",
                ChangePercent = Math.Round(changePercent, 1),
                Description = "Failure frequency is decreasing."
            };
        }

        return new TrendDto
        {
            Direction = "Stable",
            ChangePercent = Math.Round(changePercent, 1),
            Description = "Failure frequency is stable."
        };
    }

    private static string BuildRecommendation(double healthScore, double mtbfHours, TrendDto trend)
    {
        if (healthScore < 40)
        {
            return "Stop and inspect critical components before the next production run.";
        }

        if (healthScore < 60)
        {
            return $"Schedule preventive maintenance soon. Current MTBF is {mtbfHours:0.#} hours.";
        }

        if (trend.Direction == "Deteriorating")
        {
            return "Increase inspection frequency because the failure trend is deteriorating.";
        }

        return "Continue standard preventive maintenance.";
    }
}
