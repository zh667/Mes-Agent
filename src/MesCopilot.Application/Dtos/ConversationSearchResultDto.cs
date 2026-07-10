namespace MesCopilot.Application.Dtos;

public record ConversationSearchResultDto(
    Guid ConversationId,
    string Title,
    string MatchedSnippet,
    DateTime LastMessageAt,
    int MessageCount);
