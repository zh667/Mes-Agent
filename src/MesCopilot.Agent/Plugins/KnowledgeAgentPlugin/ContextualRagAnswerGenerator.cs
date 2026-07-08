using MesCopilot.Application.Dtos;

namespace MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;

public class ContextualRagAnswerGenerator : IRagAnswerGenerator
{
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
                $"No matching knowledge-base context was found for '{query}'. Please upload a related SOP or maintenance document.");
        }

        DocumentSearchResultDto topResult = context[0];
        string excerpt = Truncate(topResult.Content, MaxExcerptLength);
        string answer = $"Based on {topResult.DocumentTitle}: {excerpt}";

        return Task.FromResult(answer);
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
