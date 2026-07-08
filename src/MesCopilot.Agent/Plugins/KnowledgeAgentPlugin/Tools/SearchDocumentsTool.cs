using System.Diagnostics;
using MesCopilot.Agent.Models;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.KnowledgeAgentPlugin.Tools;

public class SearchDocumentsTool
{
    private readonly IKnowledgeService _knowledgeService;

    public SearchDocumentsTool(IKnowledgeService knowledgeService)
    {
        _knowledgeService = knowledgeService;
    }

    public string Name => "SearchDocuments";

    public string Description => "Search document metadata by keyword.";

    public async Task<FunctionCallResult> ExecuteAsync(string query, bool debugMode = false)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Search query is required.", nameof(query));
        }

        string normalizedQuery = query.Trim();
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<DocumentDto> documents = await FindMatchingDocumentsAsync(normalizedQuery);
        stopwatch.Stop();

        return new FunctionCallResult
        {
            Data = new
            {
                query = normalizedQuery,
                documents,
                totalCount = documents.Count
            },
            Explanation = $"Found {documents.Count} documents matching '{normalizedQuery}'.",
            Debug = CreateDebug(debugMode, stopwatch)
        };
    }

    private async Task<List<DocumentDto>> FindMatchingDocumentsAsync(string query)
    {
        List<DocumentDto> documents = (await _knowledgeService.GetAllAsync()).ToList();

        return documents
            .Where(document => Contains(document.Title, query) ||
                               Contains(document.FileName, query) ||
                               Contains(document.Description, query))
            .ToList();
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
