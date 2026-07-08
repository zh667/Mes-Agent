using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;
using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin.Tools;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;
using System.Text.Json;

namespace MesCopilot.UnitTests.Agent.Plugins;

public class KnowledgeAgentPluginTests
{
    [Fact]
    public async Task SearchDocumentsTool_ShouldReturnRagAnswerWithSources()
    {
        var service = new FakeKnowledgeService();
        var answerGenerator = new FakeRagAnswerGenerator("Stop the line and inspect the A102 sensor.");
        var tool = new SearchDocumentsTool(service, answerGenerator);

        var result = await tool.ExecuteAsync(
            "A102 alarm",
            debugMode: true,
            topK: 3,
            similarityThreshold: 0.6);

        Assert.NotNull(result.Data);
        string json = JsonSerializer.Serialize(result.Data);
        using JsonDocument data = JsonDocument.Parse(json);
        Assert.Equal("A102 alarm", data.RootElement.GetProperty("query").GetString());
        Assert.Equal("Stop the line and inspect the A102 sensor.", data.RootElement.GetProperty("answer").GetString());
        Assert.Equal(1, data.RootElement.GetProperty("totalCount").GetInt32());
        Assert.False(data.RootElement.TryGetProperty("prompt", out _));
        JsonElement source = data.RootElement.GetProperty("sources")[0];
        Assert.Equal("SOP A102 Alarm Handling", source.GetProperty("title").GetString());
        Assert.Equal("sop-a102.pdf", source.GetProperty("fileName").GetString());
        Assert.Contains("retrieved 1 knowledge chunks", result.Explanation);
        Assert.Equal("RAG.VectorSearch", result.Debug?.DataSource);
        Assert.Equal("SearchDocuments", result.Debug?.ToolsCalled?.Single());
        Assert.Equal("A102 alarm", service.LastSearchQuery);
        Assert.Equal(3, service.LastTopK);
        Assert.Equal(0.6, service.LastSimilarityThreshold);
        Assert.Equal("A102 alarm", answerGenerator.LastQuery);
        Assert.Single(answerGenerator.LastContext);
    }

    [Fact]
    public async Task SearchDocumentsTool_EmptyQuery_ShouldThrow()
    {
        var tool = new SearchDocumentsTool(new FakeKnowledgeService());

        await Assert.ThrowsAsync<ArgumentException>(() => tool.ExecuteAsync(""));
    }

    [Fact]
    public async Task SearchDocumentsTool_InvalidTopK_ShouldThrow()
    {
        var tool = new SearchDocumentsTool(new FakeKnowledgeService());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => tool.ExecuteAsync("alarm", topK: 0));
    }

    [Fact]
    public async Task SearchDocumentsTool_TopKAboveLimit_ShouldThrow()
    {
        var tool = new SearchDocumentsTool(new FakeKnowledgeService());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => tool.ExecuteAsync("alarm", topK: 101));
    }

    [Fact]
    public async Task GetSopByCodeTool_ShouldReturnMatchingSop()
    {
        var service = new FakeKnowledgeService();
        var tool = new GetSopByCodeTool(service);

        var result = await tool.ExecuteAsync("A102", debugMode: true);

        Assert.NotNull(result.Data);
        Assert.Contains("A102", result.Explanation);
        Assert.Equal("GetSopByCode", result.Debug?.ToolsCalled?.Single());
    }

    [Fact]
    public async Task GetSopByCodeTool_EmptyCode_ShouldThrow()
    {
        var tool = new GetSopByCodeTool(new FakeKnowledgeService());

        await Assert.ThrowsAsync<ArgumentException>(() => tool.ExecuteAsync(""));
    }

    [Fact]
    public void KnowledgeAgentPlugin_ShouldExposeKnowledgeTools()
    {
        var plugin = new KnowledgeAgentPlugin(new FakeKnowledgeService());

        Assert.Equal("KnowledgeAgent", plugin.Name);
        Assert.NotNull(plugin.SearchDocumentsTool);
        Assert.NotNull(plugin.GetSopByCodeTool);
    }

    private sealed class FakeKnowledgeService : IKnowledgeService
    {
        private static readonly List<DocumentDto> Documents =
        [
            new DocumentDto(1, "SOP A102 Alarm Handling", "sop-a102.pdf", "/docs/sop-a102.pdf", DocumentType.Sop, 2048, "application/pdf", DateTime.UtcNow, "completed", "alarm handling"),
            new DocumentDto(2, "Maintenance Guide", "maint.pdf", "/docs/maint.pdf", DocumentType.MaintenanceManual, 1024, "application/pdf", DateTime.UtcNow, "completed", "maintenance")
        ];
        private static readonly IReadOnlyList<DocumentSearchResultDto> SearchResults =
        [
            new DocumentSearchResultDto(
                10,
                1,
                "SOP A102 Alarm Handling",
                "sop-a102.pdf",
                DocumentType.Sop,
                1,
                "Alarm A102 requires stopping the line and checking the sensor.",
                4,
                "A102 alarm")
        ];

        public string? LastSearchQuery { get; private set; }

        public int? LastTopK { get; private set; }

        public double? LastSimilarityThreshold { get; private set; }

        public Task<IEnumerable<DocumentDto>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<DocumentDto>>(Documents);
        }

        public Task<DocumentDto?> GetByIdAsync(int id)
        {
            return Task.FromResult(Documents.FirstOrDefault(document => document.Id == id));
        }

        public Task<DocumentDto> CreateDocumentAsync(CreateDocumentRequest request)
        {
            throw new NotSupportedException();
        }

        public Task<DocumentDto> UploadDocumentAsync(UploadDocumentRequest request)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<DocumentSearchResultDto>> SearchSimilarAsync(
            string query,
            int topK = 5,
            double similarityThreshold = 0.7)
        {
            LastSearchQuery = query;
            LastTopK = topK;
            LastSimilarityThreshold = similarityThreshold;
            return Task.FromResult(SearchResults);
        }

        public Task<bool> DeleteAsync(int id)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeRagAnswerGenerator : IRagAnswerGenerator
    {
        private readonly string _answer;

        public FakeRagAnswerGenerator(string answer)
        {
            _answer = answer;
            LastContext = [];
        }

        public string? LastQuery { get; private set; }

        public IReadOnlyList<DocumentSearchResultDto> LastContext { get; private set; }

        public Task<string> GenerateAnswerAsync(
            string query,
            IReadOnlyList<DocumentSearchResultDto> context)
        {
            LastQuery = query;
            LastContext = context;
            return Task.FromResult(_answer);
        }
    }
}
