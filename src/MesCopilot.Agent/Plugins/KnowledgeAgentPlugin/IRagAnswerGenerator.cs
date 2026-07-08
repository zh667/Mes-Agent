using MesCopilot.Application.Dtos;

namespace MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;

public interface IRagAnswerGenerator
{
    Task<string> GenerateAnswerAsync(
        string query,
        IReadOnlyList<DocumentSearchResultDto> context);
}
