using MesCopilot.Domain.Enums;

namespace MesCopilot.Application.Services.Auditing;

public sealed record AgentAuditRecord(
    string UserId,
    Guid? ConversationId,
    AgentMode Mode,
    string Status,
    string Query,
    string? ToolName,
    bool? IsVerified,
    IReadOnlyList<string>? Discrepancies,
    long DurationMilliseconds,
    string? CorrelationId);

public interface IAgentAuditService
{
    Task RecordAsync(AgentAuditRecord record, CancellationToken cancellationToken = default);
}
