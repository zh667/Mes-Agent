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
    object? Verification,
    DateTime CreatedAt);

public sealed record ConversationMessageVerificationSourceDto(
    Guid MessageId,
    AgentMode Mode,
    string Content,
    string? ToolResults,
    string? VerificationJson);

public record ConversationDetailDto(
    Guid Id,
    AgentMode Mode,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ConversationMessageDto> Messages);
