using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesCopilot.Domain.Entities.Auditing;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;

namespace MesCopilot.Application.Services.Auditing;

public sealed class AgentAuditService : IAgentAuditService
{
    private readonly MesDbContext _context;
    private readonly ITenantContext _tenantContext;

    public AgentAuditService(MesDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task RecordAsync(AgentAuditRecord record, CancellationToken cancellationToken = default)
    {
        string tenantId = _tenantContext.TenantId ??
            throw new InvalidOperationException("A tenant context is required for agent auditing.");

        _context.AgentAuditLogs.Add(new AgentAuditLog
        {
            TenantId = tenantId,
            UserId = record.UserId,
            ConversationId = record.ConversationId,
            Mode = record.Mode,
            Status = record.Status,
            ToolName = record.ToolName,
            QueryDigest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(record.Query.Trim()))),
            IsVerified = record.IsVerified,
            Discrepancies = record.Discrepancies is null
                ? null
                : JsonSerializer.Serialize(record.Discrepancies),
            DurationMilliseconds = record.DurationMilliseconds,
            CorrelationId = record.CorrelationId
        });
        await _context.SaveChangesAsync(cancellationToken);
    }
}
