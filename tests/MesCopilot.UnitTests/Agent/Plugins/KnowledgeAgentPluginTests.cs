using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;
using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin.Tools;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Agent.Plugins;

public class KnowledgeAgentPluginTests
{
    [Fact]
    public async Task SearchDocumentsTool_ShouldReturnMatchingDocuments()
    {
        var service = new FakeKnowledgeService();
        var tool = new SearchDocumentsTool(service);

        var result = await tool.ExecuteAsync("alarm", debugMode: true);

        Assert.NotNull(result.Data);
        Assert.Contains("1", result.Explanation);
        Assert.Equal("SearchDocuments", result.Debug?.ToolsCalled?.Single());
    }

    [Fact]
    public async Task SearchDocumentsTool_EmptyQuery_ShouldThrow()
    {
        var tool = new SearchDocumentsTool(new FakeKnowledgeService());

        await Assert.ThrowsAsync<ArgumentException>(() => tool.ExecuteAsync(""));
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

        public Task<bool> DeleteAsync(int id)
        {
            throw new NotSupportedException();
        }
    }
}
