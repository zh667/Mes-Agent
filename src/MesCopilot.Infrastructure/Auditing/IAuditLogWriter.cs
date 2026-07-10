using MesCopilot.Domain.Entities.Auditing;

namespace MesCopilot.Infrastructure.Auditing;

public interface IAuditLogWriter
{
    Task WriteAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
}
