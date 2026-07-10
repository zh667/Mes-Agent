using MesCopilot.Domain.Enums;

namespace MesCopilot.Agent.Verification;

public sealed record VerificationContext(
    string TenantId,
    string UserId,
    AgentMode AgentMode,
    string ToolName,
    DateTime FromUtc,
    DateTime ToUtc,
    string CorrelationId);
