using System.Diagnostics;
using System.Globalization;
using MesCopilot.Agent.Caching;
using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;
using MesCopilot.Agent.Models;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.KnowledgeAgentPlugin.Tools;

public class SearchDocumentsTool
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    private readonly IKnowledgeService _knowledgeService;
    private readonly IRagAnswerGenerator _answerGenerator;
    private readonly IAgentResponseCache? _responseCache;

    public SearchDocumentsTool(IKnowledgeService knowledgeService)
        : this(knowledgeService, new ContextualRagAnswerGenerator())
    {
    }

    public SearchDocumentsTool(
        IKnowledgeService knowledgeService,
        IRagAnswerGenerator answerGenerator)
        : this(knowledgeService, answerGenerator, null)
    {
    }

    public SearchDocumentsTool(
        IKnowledgeService knowledgeService,
        IRagAnswerGenerator answerGenerator,
        IAgentResponseCache? responseCache)
    {
        _knowledgeService = knowledgeService;
        _answerGenerator = answerGenerator;
        _responseCache = responseCache;
    }

    public string Name => "SearchDocuments";

    public string Description => "Search the knowledge base with RAG vector retrieval and return an answer with sources.";

    public async Task<FunctionCallResult> ExecuteAsync(
        string query,
        bool debugMode = false,
        int topK = 5,
        double similarityThreshold = 0.7)
    {
        string normalizedQuery = ValidateArguments(query, topK, similarityThreshold);
        string cacheKey = BuildCacheKey(normalizedQuery, topK, similarityThreshold);
        Stopwatch stopwatch = Stopwatch.StartNew();
        if (_responseCache is not null &&
            _responseCache.TryGet(cacheKey, out FunctionCallResult cachedResult))
        {
            stopwatch.Stop();
            return new FunctionCallResult
            {
                Data = cachedResult.Data,
                Explanation = cachedResult.Explanation,
                Debug = CreateDebug(debugMode, stopwatch, "Agent.ResponseCache")
            };
        }

        IReadOnlyList<DocumentSearchResultDto> chunks = await _knowledgeService.SearchSimilarAsync(
            normalizedQuery,
            topK,
            similarityThreshold);
        string answer = await _answerGenerator.GenerateAnswerAsync(normalizedQuery, chunks);
        stopwatch.Stop();
        var sources = BuildSources(chunks);
        var chunkData = chunks.Select(chunk => new
        {
            chunkId = chunk.ChunkId,
            documentId = chunk.DocumentId,
            documentTitle = chunk.DocumentTitle,
            fileName = chunk.FileName,
            documentType = chunk.DocumentType.ToString(),
            sequence = chunk.Sequence,
            content = chunk.Content,
            pageNumber = chunk.PageNumber,
            sectionTitle = chunk.SectionTitle
        }).ToList();

        var result = new FunctionCallResult
        {
            Data = new
            {
                query = normalizedQuery,
                answer,
                sources,
                chunks = chunkData,
                totalCount = chunks.Count
            },
            Explanation = $"RAG search retrieved {chunks.Count} knowledge chunks for '{normalizedQuery}' from {sources.Count} sources.",
            Debug = CreateDebug(debugMode, stopwatch)
        };

        _responseCache?.Set(cacheKey, result, CacheDuration);
        return result;
    }

    private static string ValidateArguments(string query, int topK, double similarityThreshold)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Search query is required.", nameof(query));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);
        if (topK > KnowledgeSearchLimits.MaxTopK)
        {
            throw new ArgumentOutOfRangeException(nameof(topK), $"TopK must be less than or equal to {KnowledgeSearchLimits.MaxTopK}.");
        }

        if (similarityThreshold < 0d || similarityThreshold > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(similarityThreshold), "Similarity threshold must be in the range [0, 1].");
        }

        return query.Trim();
    }

    private static List<object> BuildSources(IReadOnlyList<DocumentSearchResultDto> chunks)
    {
        return chunks
            .GroupBy(chunk => chunk.DocumentId)
            .Select(group =>
            {
                DocumentSearchResultDto firstChunk = group.First();
                return (object)new
                {
                    documentId = firstChunk.DocumentId,
                    title = firstChunk.DocumentTitle,
                    fileName = firstChunk.FileName,
                    documentType = firstChunk.DocumentType.ToString(),
                    chunkCount = group.Count()
                };
            })
            .ToList();
    }

    private static string BuildCacheKey(string query, int topK, double similarityThreshold)
    {
        return string.Join(
            "|",
            query,
            topK.ToString(CultureInfo.InvariantCulture),
            similarityThreshold.ToString("R", CultureInfo.InvariantCulture));
    }

    private DebugInfo? CreateDebug(
        bool debugMode,
        Stopwatch stopwatch,
        string dataSource = "RAG.VectorSearch")
    {
        return debugMode ? new DebugInfo
        {
            ExecutionTime = $"{stopwatch.ElapsedMilliseconds}ms",
            DataSource = dataSource,
            ToolsCalled = new List<string> { Name }
        } : null;
    }
}
