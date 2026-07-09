using System.Diagnostics;
using MesCopilot.Agent.Models;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.QualityAgentPlugin.Tools;

public class GetDefectsByProcessTool
{
    private readonly IQualityService _qualityService;

    public GetDefectsByProcessTool(IQualityService qualityService)
    {
        _qualityService = qualityService;
    }

    public string Name => "GetDefectsByProcess";

    public string Description => "Summarize failed quantities for a process step.";

    public async Task<FunctionCallResult> ExecuteAsync(int processStepId, bool debugMode = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processStepId);

        Stopwatch stopwatch = Stopwatch.StartNew();
        List<Application.Dtos.QualityInspectionDto> inspections = (await _qualityService.GetInspectionsAsync())
            .Where(inspection => inspection.ProcessStepId == processStepId)
            .ToList();
        stopwatch.Stop();

        int failedQuantity = inspections.Sum(inspection => inspection.FailedQuantity);
        var data = new
        {
            processStepId,
            failedQuantity,
            inspectionCount = inspections.Count,
            inspections
        };

        return new FunctionCallResult
        {
            Data = data,
            Explanation = $"Process step {processStepId} has {failedQuantity} failed units across {inspections.Count} inspections.",
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
