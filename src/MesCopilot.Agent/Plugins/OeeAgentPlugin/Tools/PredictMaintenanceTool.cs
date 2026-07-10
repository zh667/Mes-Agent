using System.Diagnostics;
using MesCopilot.Agent.Models;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.OeeAgentPlugin.Tools;

public class PredictMaintenanceTool
{
    private readonly IMaintenancePredictionService _predictionService;

    public PredictMaintenanceTool(IMaintenancePredictionService predictionService)
    {
        _predictionService = predictionService;
    }

    public string Name => "PredictMaintenance";

    public string Description => "Predict equipment maintenance needs using downtime history, MTBF, MTTR, and health score.";

    public async Task<FunctionCallResult> ExecuteAsync(int equipmentId, bool debugMode = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(equipmentId);

        Stopwatch stopwatch = Stopwatch.StartNew();
        var prediction = await _predictionService.PredictMaintenanceAsync(equipmentId);
        stopwatch.Stop();

        string predictedFailureText = prediction.PredictedNextFailureAt.HasValue
            ? $" Predicted next failure: {prediction.PredictedNextFailureAt:yyyy-MM-dd HH:mm}."
            : string.Empty;

        return new FunctionCallResult
        {
            Data = prediction,
            Explanation = $"Equipment {prediction.EquipmentCode} ({prediction.EquipmentName}) health score is {prediction.HealthScore:0.#}/100 ({prediction.HealthLevel}). MTBF={prediction.MtbfHours:0.#}h, MTTR={prediction.MttrHours:0.#}h.{predictedFailureText} Recommendation: {prediction.MaintenanceRecommendation}",
            Debug = debugMode ? new DebugInfo
            {
                ExecutionTime = $"{stopwatch.ElapsedMilliseconds}ms",
                DataSource = "MesCopilot.Database",
                ToolsCalled = [Name]
            } : null
        };
    }
}
