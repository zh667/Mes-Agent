using System.Diagnostics;
using MesCopilot.Agent.Models;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.OeeAgentPlugin.Tools;

public class AnalyzeLowOeeTool
{
    private readonly IEquipmentService _equipmentService;

    public AnalyzeLowOeeTool(IEquipmentService equipmentService)
    {
        _equipmentService = equipmentService;
    }

    public string Name => "AnalyzeLowOee";

    public string Description => "Find equipment whose OEE falls below a threshold.";

    public async Task<FunctionCallResult> ExecuteAsync(DateTime date, decimal threshold = 0.75m, bool debugMode = false)
    {
        DateTime normalizedDate = ValidateArguments(date, threshold);

        Stopwatch stopwatch = Stopwatch.StartNew();
        List<EquipmentDto> equipment = (await _equipmentService.GetAllAsync()).Where(item => item.IsActive).ToList();
        Task<EquipmentOeeSnapshot>[] metricTasks = equipment
            .Select(item => BuildSnapshotAsync(item, normalizedDate))
            .ToArray();
        EquipmentOeeSnapshot[] snapshots = await Task.WhenAll(metricTasks);
        List<EquipmentOeeSnapshot> lowOeeEquipment = snapshots
            .Where(snapshot => snapshot.Oee < threshold)
            .ToList();

        stopwatch.Stop();

        return new FunctionCallResult
        {
            Data = new
            {
                date = normalizedDate,
                threshold,
                lowOeeEquipment,
                totalCount = lowOeeEquipment.Count
            },
            Explanation = $"{lowOeeEquipment.Count} equipment items are below the OEE threshold of {threshold * 100:0.#}%.",
            Debug = CreateDebug(debugMode, stopwatch)
        };
    }

    private static DateTime ValidateArguments(DateTime date, decimal threshold)
    {
        DateTime normalizedDate = date.Date;
        if (normalizedDate > DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Future dates are not supported.", nameof(date));
        }

        if (threshold <= 0m || threshold > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be in the range (0, 1].");
        }

        return normalizedDate;
    }

    private async Task<EquipmentOeeSnapshot> BuildSnapshotAsync(EquipmentDto equipment, DateTime date)
    {
        OeeDto oee = await _equipmentService.CalculateOeeAsync(equipment.Id, date);
        return new EquipmentOeeSnapshot(
            equipment.Id,
            equipment.Code,
            equipment.Name,
            oee.Oee,
            oee.Availability,
            oee.Performance,
            oee.Quality);
    }

    private DebugInfo? CreateDebug(bool debugMode, Stopwatch stopwatch)
    {
        return debugMode ? new DebugInfo
        {
            ExecutionTime = $"{stopwatch.ElapsedMilliseconds}ms",
            DataSource = "MesCopilot.Database",
            ToolsCalled = new List<string> { Name }
        } : null;
    }

    private record EquipmentOeeSnapshot(
        int Id,
        string Code,
        string Name,
        decimal Oee,
        decimal Availability,
        decimal Performance,
        decimal Quality
    );
}
