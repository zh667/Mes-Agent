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

        var firstReport = trace.ProductionReports.FirstOrDefault();
        var workOrder = firstReport is null
            ? null
            : new
            {
                id = firstReport.WorkOrderId,
                batchNumber = trace.BatchNumber
            };
        var processSteps = trace.ProductionReports
            .DistinctBy(report => report.ProcessStepId)
            .Select(report => new
            {
                id = report.ProcessStepId,
                report.Timestamp,
                report.OperatorId,
                report.OperatorName,
                report.Quantity,
                report.QualifiedQuantity
            })
            .ToList();
        var equipment = trace.ProductionReports
            .Select(report => new
            {
                id = report.EquipmentId
            })
            .Distinct()
            .ToList();

        var data = new
        {
            trace.BatchNumber,
            trace.ProductionReports,
            trace.Inspections,
            workOrder,
            processSteps,
            equipment,
            productionReportCount = trace.ProductionReports.Count,
            inspectionCount = trace.Inspections.Count
        };

        return new FunctionCallResult
        {
            Data = data,
            Explanation = $"Batch {normalizedBatchNumber} trace（追溯） has {data.productionReportCount} production reports and {data.inspectionCount} inspections.",
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
