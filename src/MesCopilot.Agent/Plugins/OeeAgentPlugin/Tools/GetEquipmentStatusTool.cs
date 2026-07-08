using System.Diagnostics;
using MesCopilot.Agent.Models;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.OeeAgentPlugin.Tools;

public class GetEquipmentStatusTool
{
    private readonly IEquipmentService _equipmentService;

    public GetEquipmentStatusTool(IEquipmentService equipmentService)
    {
        _equipmentService = equipmentService;
    }

    public string Name => "GetEquipmentStatus";

    public string Description => "Get current status history and alarms for equipment.";

    public async Task<FunctionCallResult> ExecuteAsync(int equipmentId, bool debugMode = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(equipmentId);

        Stopwatch stopwatch = Stopwatch.StartNew();
        Application.Dtos.EquipmentDto? equipment = await _equipmentService.GetByIdAsync(equipmentId);
        List<Application.Dtos.EquipmentStatusDto> statuses = (await _equipmentService.GetStatusHistoryAsync(equipmentId)).ToList();
        List<Application.Dtos.EquipmentAlarmDto> alarms = (await _equipmentService.GetAlarmsAsync(equipmentId)).ToList();
        stopwatch.Stop();

        if (equipment is null)
        {
            return new FunctionCallResult
            {
                Data = null,
                Explanation = $"Equipment {equipmentId} was not found.",
                Debug = CreateDebug(debugMode, stopwatch)
            };
        }

        Application.Dtos.EquipmentStatusDto? latestStatus = statuses.OrderByDescending(status => status.StartTime).FirstOrDefault();
        return new FunctionCallResult
        {
            Data = new
            {
                equipment,
                latestStatus,
                statuses,
                alarms,
                openAlarmCount = alarms.Count(alarm => alarm.ResolvedAt is null)
            },
            Explanation = $"Equipment {equipment.Code} latest status is {latestStatus?.State.ToString() ?? "Unknown"} with {alarms.Count} alarms.",
            Debug = CreateDebug(debugMode, stopwatch)
        };
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
}
