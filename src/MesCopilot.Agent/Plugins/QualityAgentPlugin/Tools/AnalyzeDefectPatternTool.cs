using System.Diagnostics;
using MesCopilot.Agent.Models;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.QualityAgentPlugin.Tools;

public class AnalyzeDefectPatternTool
{
    private readonly IQualityService _qualityService;

    public AnalyzeDefectPatternTool(IQualityService qualityService)
    {
        _qualityService = qualityService;
    }

    public string Name => "AnalyzeDefectPattern";

    public string Description => "Analyze defect quantities and identify the leading defect type.";

    public async Task<FunctionCallResult> ExecuteAsync(bool debugMode = false)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<Application.Dtos.DefectAnalysisDto> defects = (await _qualityService.AnalyzeDefectsAsync()).ToList();
        stopwatch.Stop();

        Application.Dtos.DefectAnalysisDto? topDefect = defects.OrderByDescending(defect => defect.TotalQuantity).FirstOrDefault();
        string explanation = topDefect is null
            ? "No defect records found."
            : $"Top defect is {topDefect.DefectTypeName} with {topDefect.TotalQuantity} occurrences.";

        return new FunctionCallResult
        {
            Data = new
            {
                defects,
                totalDefectQuantity = defects.Sum(defect => defect.TotalQuantity),
                topDefect
            },
            Explanation = explanation,
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
