using MesCopilot.Domain.Enums;

namespace MesCopilot.Domain.Entities.Conversations;

public class ConversationMessage : MesCopilot.Domain.Common.ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;

    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public MessageRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? ToolResults { get; set; }

    public string? VerificationJson { get; set; }

    public int? VerificationSchemaVersion { get; set; }

    public DateTime CreatedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
}
