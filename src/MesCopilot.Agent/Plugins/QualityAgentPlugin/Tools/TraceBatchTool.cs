using System.Diagnostics;
using MesCopilot.Agent.Models;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.QualityAgentPlugin.Tools;

public class TraceBatchTool
{
    private readonly IQualityService _qualityService;

    public TraceBatchTool(IQualityService qualityService)
    {
        _qualityService = qualityService;
    }

    public string Name => "TraceBatch";

    public string Description => "Trace production reports and inspections for a batch.";

    public async Task<FunctionCallResult> ExecuteAsync(string batchNumber, bool debugMode = false)
    {
        if (string.IsNullOrWhiteSpace(batchNumber))
        {
            throw new ArgumentException("Batch number is required.", nameof(batchNumber));
        }

        string normalizedBatchNumber = batchNumber.Trim();
        Stopwatch stopwatch = Stopwatch.StartNew();
        Application.Dtos.BatchTraceDto trace = await _qualityService.TraceBatchAsync(normalizedBatchNumber);
        stopwatch.Stop();

        var data = new
        {
            trace.BatchNumber,
            trace.ProductionReports,
            trace.Inspections,
            productionReportCount = trace.ProductionReports.Count,
            inspectionCount = trace.Inspections.Count
        };

        return new FunctionCallResult
        {
            Data = data,
            Explanation = $"Batch {normalizedBatchNumber} has {data.productionReportCount} production reports and {data.inspectionCount} inspections.",
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
