using MesCopilot.Domain.Enums;

namespace MesCopilot.Domain.Entities.Conversations;

public class Conversation : MesCopilot.Domain.Common.ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;

    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public AgentMode Mode { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();
}
