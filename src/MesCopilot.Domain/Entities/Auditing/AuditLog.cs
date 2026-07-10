using MesCopilot.Domain.Common;

namespace MesCopilot.Domain.Entities.Auditing;

public sealed class AuditLog : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string RouteTemplate { get; set; } = string.Empty;
    public string QueryKeys { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long DurationMilliseconds { get; set; }
    public string? RequestBody { get; set; }
    public string? CorrelationId { get; set; }
    public string? IpHash { get; set; }
}
