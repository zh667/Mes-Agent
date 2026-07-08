using System.Diagnostics;
using MesCopilot.Agent.Models;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.OeeAgentPlugin.Tools;

public class CalculateOeeTool
{
    private readonly IEquipmentService _equipmentService;

    public CalculateOeeTool(IEquipmentService equipmentService)
    {
        _equipmentService = equipmentService;
    }

    public string Name => "CalculateOee";

    public string Description => "Calculate OEE metrics for one equipment item.";

    public async Task<FunctionCallResult> ExecuteAsync(int equipmentId, DateTime date, bool debugMode = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(equipmentId);
        DateTime normalizedDate = ValidateNotFutureDate(date);

        Stopwatch stopwatch = Stopwatch.StartNew();
        Application.Dtos.OeeDto oee = await _equipmentService.CalculateOeeAsync(equipmentId, normalizedDate);
        stopwatch.Stop();

        return new FunctionCallResult
        {
            Data = oee,
            Explanation = $"Equipment {equipmentId} OEE is {oee.Oee * 100:0.#}% on {normalizedDate:yyyy-MM-dd}.",
            Debug = CreateDebug(debugMode, stopwatch)
        };
    }

    private static DateTime ValidateNotFutureDate(DateTime date)
    {
        DateTime normalizedDate = date.Date;
        if (normalizedDate > DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Future dates are not supported.", nameof(date));
        }

        return normalizedDate;
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
