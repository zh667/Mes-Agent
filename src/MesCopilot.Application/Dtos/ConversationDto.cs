using MesCopilot.Domain.Enums;

namespace MesCopilot.Application.Dtos;

public record ConversationDto(
    Guid Id,
    AgentMode Mode,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int MessageCount);

public record ConversationMessageDto(
    Guid Id,
    MessageRole Role,
    string Content,
    object? ToolResults,
    DateTime CreatedAt);

public record ConversationDetailDto(
    Guid Id,
    AgentMode Mode,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ConversationMessageDto> Messages);
