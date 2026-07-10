using MesCopilot.Domain.Common;
using MesCopilot.Domain.Enums;

namespace MesCopilot.Domain.Entities.Auditing;

public sealed class AgentAuditLog : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = string.Empty;
    public Guid? ConversationId { get; set; }
    public AgentMode Mode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ToolName { get; set; }
    public string? QueryDigest { get; set; }
    public bool? IsVerified { get; set; }
    public string? Discrepancies { get; set; }
    public long DurationMilliseconds { get; set; }
    public string? CorrelationId { get; set; }
}
