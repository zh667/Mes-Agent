using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;
using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Agent.Plugins;

public class VerifiedRagAnswerGeneratorTests
{
    private readonly VerifiedRagAnswerGenerator _generator = new();

    [Fact]
    public async Task GenerateAnswerAsync_WithRelevantChunks_IncludesTopThreeCitations()
    {
        DocumentSearchResultDto[] chunks =
        [
            CreateChunk("E-201 每周需检查主轴润滑油位", "维护手册.pdf", pageNumber: 12),
            CreateChunk("润滑油更换周期500小时", "维护手册.pdf", pageNumber: 13),
            CreateChunk("日常点检项目列表", "保养计划.pdf", pageNumber: 5),
            CreateChunk("不应使用的第四段内容", "extra.pdf", pageNumber: 1)
        ];

        string answer = await _generator.GenerateAnswerAsync("E-201维护", chunks);

        Assert.Contains("[来源:", answer);
        Assert.Contains("维护手册.pdf, 第12页", answer);
        Assert.Contains("维护手册.pdf, 第13页", answer);
        Assert.Contains("保养计划.pdf, 第5页", answer);
        Assert.DoesNotContain("extra.pdf", answer);
    }

    [Fact]
    public async Task GenerateAnswerAsync_WithNoChunks_ReturnsNotFound()
    {
        string answer = await _generator.GenerateAnswerAsync("E-201维护", []);

        Assert.Contains("未找到", answer);
        Assert.Contains("E-201维护", answer);
    }

    [Fact]
    public async Task GenerateAnswerAsync_WithNullPageNumber_OmitsPageText()
    {
        DocumentSearchResultDto[] chunks =
        [
            CreateChunk("文本内容", "readme.txt", pageNumber: null)
        ];

        string answer = await _generator.GenerateAnswerAsync("问题", chunks);

        Assert.Contains("readme.txt", answer);
        Assert.DoesNotContain("第页", answer);
    }

    [Fact]
    public async Task GenerateAnswerAsync_BlankQuestion_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _generator.GenerateAnswerAsync(" ", []));
    }

    private static DocumentSearchResultDto CreateChunk(string content, string fileName, int? pageNumber)
    {
        return new DocumentSearchResultDto(
            ChunkId: Random.Shared.Next(1, 10_000),
            DocumentId: Random.Shared.Next(1, 10_000),
            DocumentTitle: fileName,
            FileName: fileName,
            DocumentType: DocumentType.Sop,
            Sequence: 1,
            Content: content,
            PageNumber: pageNumber,
            SectionTitle: "section");
    }
}
