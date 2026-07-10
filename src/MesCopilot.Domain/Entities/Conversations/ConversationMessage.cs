using MesCopilot.Domain.Enums;

namespace MesCopilot.Domain.Entities.Conversations;

public class ConversationMessage
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public MessageRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? ToolResults { get; set; }

    public DateTime CreatedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
}
