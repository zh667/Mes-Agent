using System.Diagnostics;
using MesCopilot.Agent.Models;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;

namespace MesCopilot.Agent.Plugins.KnowledgeAgentPlugin.Tools;

public class GetSopByCodeTool
{
    private readonly IKnowledgeService _knowledgeService;

    public GetSopByCodeTool(IKnowledgeService knowledgeService)
    {
        _knowledgeService = knowledgeService;
    }

    public string Name => "GetSopByCode";

    public string Description => "Find a SOP document by code in its title or file name.";

    public async Task<FunctionCallResult> ExecuteAsync(string code, bool debugMode = false)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("SOP code is required.", nameof(code));
        }

        string normalizedCode = code.Trim();
        Stopwatch stopwatch = Stopwatch.StartNew();
        IEnumerable<Application.Dtos.DocumentDto> documents = await _knowledgeService.GetAllAsync();
        Application.Dtos.DocumentDto? sop = documents.FirstOrDefault(document =>
            document.Type == DocumentType.Sop &&
            (Contains(document.Title, normalizedCode) || Contains(document.FileName, normalizedCode)));
        stopwatch.Stop();

        return new FunctionCallResult
        {
            Data = sop,
            Explanation = sop is null
                ? $"No SOP document matched code {normalizedCode}."
                : $"Found SOP {sop.Title} for code {normalizedCode}.",
            Debug = CreateDebug(debugMode, stopwatch)
        };
    }

    private static bool Contains(string? value, string query)
    {
        return value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
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
