using System.Text;
using MesCopilot.Application.Dtos;

namespace MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;

public class VerifiedRagAnswerGenerator : IRagAnswerGenerator
{
    private const int TopK = 3;
    private const int MaxExcerptLength = 600;

    public Task<string> GenerateAnswerAsync(
        string query,
        IReadOnlyList<DocumentSearchResultDto> context)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Search query is required.", nameof(query));
        }

        if (context.Count == 0)
        {
            return Task.FromResult(
                $"未找到与“{query}”相关的知识库文档，请上传相关 SOP、维护手册或工艺文件后重试。");
        }

        List<DocumentSearchResultDto> selectedChunks = context
            .Take(TopK)
            .ToList();

        StringBuilder answer = new();
        answer.AppendLine("根据知识库检索结果：");
        foreach (DocumentSearchResultDto chunk in selectedChunks)
        {
            answer.AppendLine($"- {Truncate(chunk.Content, MaxExcerptLength)}");
        }

        answer.AppendLine();
        answer.Append("[来源: ");
        answer.Append(string.Join("; ", selectedChunks.Select(FormatCitation)));
        answer.Append(']');

        return Task.FromResult(answer.ToString());
    }

    private static string FormatCitation(DocumentSearchResultDto chunk)
    {
        if (chunk.PageNumber.HasValue)
        {
            return $"{chunk.DocumentTitle}, 第{chunk.PageNumber.Value}页";
        }

        return chunk.DocumentTitle;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }
}
