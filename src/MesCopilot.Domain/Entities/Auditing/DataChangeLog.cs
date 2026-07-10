using MesCopilot.Domain.Common;

namespace MesCopilot.Domain.Entities.Auditing;

public sealed class DataChangeLog : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string NewValues { get; set; } = "{}";
    public string? CorrelationId { get; set; }
}
