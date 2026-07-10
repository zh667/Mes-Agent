using MesCopilot.Domain.Enums;

namespace MesCopilot.Api.Dtos.Agent;

public record ChatRequest(
    Guid? ConversationId,
    AgentMode Mode,
    string Message,
    bool DebugMode = false);
