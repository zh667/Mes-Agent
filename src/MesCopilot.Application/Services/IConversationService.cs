using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Enums;

namespace MesCopilot.Application.Services;

public interface IConversationService
{
    Task<ConversationDto> CreateConversationAsync(string userId, AgentMode mode, string initialMessage);

    Task<ConversationDetailDto?> GetConversationAsync(Guid conversationId, string userId);

    Task<IReadOnlyList<ConversationDto>> GetUserConversationsAsync(string userId, int page = 1, int pageSize = 20);

    Task<IReadOnlyList<ConversationMessageDto>> GetContextWindowAsync(Guid conversationId, int maxMessages = 10);

    Task AddMessageAsync(Guid conversationId, MessageRole role, string content, string? toolResults = null);

    Task DeleteConversationAsync(Guid conversationId, string userId);

    Task<IReadOnlyList<ConversationSearchResultDto>> SearchConversationsAsync(
        string query,
        string userId,
        int skip = 0,
        int take = 20);
}
