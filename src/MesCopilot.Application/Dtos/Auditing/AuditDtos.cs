using MesCopilot.Domain.Enums;

namespace MesCopilot.Application.Dtos.Auditing;

public sealed record PagedAuditResultDto<T>(IReadOnlyList<T> Items, int Total, int Skip, int Take);

public sealed record AuditLogDto(
    Guid Id,
    DateTime Timestamp,
    string? UserId,
    string Method,
    string RouteTemplate,
    string QueryKeys,
    int StatusCode,
    long DurationMilliseconds,
    string? RequestBody,
    string? CorrelationId,
    string? IpHash);

public sealed record DataChangeLogDto(
    Guid Id,
    DateTime Timestamp,
    string? UserId,
    string EntityType,
    string EntityId,
    string ChangeType,
    string? OldValues,
    string NewValues,
    string? CorrelationId);

public sealed record AgentAuditLogDto(
    Guid Id,
    DateTime Timestamp,
    string UserId,
    Guid? ConversationId,
    AgentMode Mode,
    string Status,
    string? ToolName,
    string? QueryDigest,
    bool? IsVerified,
    string? Discrepancies,
    long DurationMilliseconds,
    string? CorrelationId);

public sealed record AuditReportRowDto(
    DateTime Timestamp,
    string Category,
    string Actor,
    string Action,
    string Target,
    string Status,
    long DurationMilliseconds);
