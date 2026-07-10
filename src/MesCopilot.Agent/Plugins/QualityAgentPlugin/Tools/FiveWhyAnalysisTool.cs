using System.Diagnostics;
using MesCopilot.Agent.Models;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.QualityAgentPlugin.Tools;

public class FiveWhyAnalysisTool
{
    private readonly IQualityRootCauseService _rootCauseService;

    public FiveWhyAnalysisTool(IQualityRootCauseService rootCauseService)
    {
        _rootCauseService = rootCauseService;
    }

    public string Name => "FiveWhyAnalysis";

    public string Description => "Build a 5-Why quality root-cause chain with evidence and corrective actions.";

    public async Task<FunctionCallResult> ExecuteAsync(
        int? defectRecordId,
        string? symptomDescription,
        bool debugMode = false)
    {
        if (!defectRecordId.HasValue && string.IsNullOrWhiteSpace(symptomDescription))
        {
            throw new ArgumentException("Provide either defectRecordId or symptomDescription.", nameof(symptomDescription));
        }

        if (defectRecordId.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(defectRecordId.Value, nameof(defectRecordId));
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        var analysis = await _rootCauseService.AnalyzeAsync(defectRecordId, symptomDescription);
        stopwatch.Stop();

        string firstAnswer = analysis.WhyChain.FirstOrDefault()?.Answer ?? symptomDescription ?? "Unknown symptom";

        return new FunctionCallResult
        {
            Data = analysis,
            Explanation = $"5-Why analysis completed for {firstAnswer}. Root cause: {analysis.RootCause}. Recommended action: {analysis.CorrectiveActions.FirstOrDefault() ?? "Collect more evidence."}",
            Debug = debugMode ? new DebugInfo
            {
                ExecutionTime = $"{stopwatch.ElapsedMilliseconds}ms",
                DataSource = "MesCopilot.Database",
                ToolsCalled = [Name]
            } : null
        };
    }
}
