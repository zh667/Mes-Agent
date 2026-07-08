using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;
using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Agent.Plugins;

public class ContextualRagAnswerGeneratorTests
{
    [Fact]
    public async Task GenerateAnswerAsync_EmptyContext_ShouldReturnNoContextMessage()
    {
        var generator = new ContextualRagAnswerGenerator();

        string answer = await generator.GenerateAnswerAsync("alarm A102", []);

        Assert.Contains("No matching knowledge-base context", answer);
        Assert.Contains("alarm A102", answer);
    }

    [Fact]
    public async Task GenerateAnswerAsync_WithContext_ShouldUseTopDocumentAndTruncateLongContent()
    {
        var generator = new ContextualRagAnswerGenerator();
        string longContent = new('a', 650);
        DocumentSearchResultDto result = CreateSearchResult(longContent);

        string answer = await generator.GenerateAnswerAsync("alarm A102", [result]);

        Assert.StartsWith("Based on SOP A102:", answer);
        Assert.EndsWith("...", answer);
        Assert.DoesNotContain(new string('a', 650), answer);
    }

    [Fact]
    public async Task GenerateAnswerAsync_BlankQuery_ShouldThrow()
    {
        var generator = new ContextualRagAnswerGenerator();

        await Assert.ThrowsAsync<ArgumentException>(() => generator.GenerateAnswerAsync(" ", []));
    }

    private static DocumentSearchResultDto CreateSearchResult(string content)
    {
        return new DocumentSearchResultDto(
            ChunkId: 10,
            DocumentId: 1,
            DocumentTitle: "SOP A102",
            FileName: "sop-a102.pdf",
            DocumentType: DocumentType.Sop,
            Sequence: 1,
            Content: content,
            PageNumber: 4,
            SectionTitle: "alarm");
    }
}
