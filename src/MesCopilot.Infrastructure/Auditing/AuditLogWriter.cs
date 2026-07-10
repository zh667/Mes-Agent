using MesCopilot.Domain.Entities.Auditing;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;

namespace MesCopilot.Infrastructure.Auditing;

public sealed class AuditLogWriter : IAuditLogWriter
{
    private readonly MesDbContext _context;
    private readonly CurrentTenantContext _tenantContext;

    public AuditLogWriter(MesDbContext context, CurrentTenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task WriteAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_tenantContext.TenantId))
        {
            _tenantContext.Initialize(new TenantResolution(auditLog.TenantId, IsPlatformAdmin: false));
        }

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
