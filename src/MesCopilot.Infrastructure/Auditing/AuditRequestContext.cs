namespace MesCopilot.Infrastructure.Auditing;

public sealed class AuditRequestContext
{
    public string? UserId { get; private set; }
    public string? CorrelationId { get; private set; }

    public void Initialize(string? userId, string correlationId)
    {
        UserId = userId;
        CorrelationId = correlationId;
    }
}
