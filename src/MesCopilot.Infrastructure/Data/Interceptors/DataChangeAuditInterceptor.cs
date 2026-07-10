using MesCopilot.Domain.Common;
using MesCopilot.Domain.Entities.Auditing;
using MesCopilot.Infrastructure.Auditing;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MesCopilot.Infrastructure.Data.Interceptors;

public sealed class DataChangeAuditInterceptor : SaveChangesInterceptor
{
    private readonly AuditRedactor _redactor;

    public DataChangeAuditInterceptor(AuditRedactor redactor)
    {
        _redactor = redactor;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditEntries(DbContext? context)
    {
        if (context is not MesDbContext mesContext || string.IsNullOrWhiteSpace(mesContext.CurrentTenantId))
        {
            return;
        }

        context.ChangeTracker.DetectChanges();
        List<DataChangeLog> auditEntries = context.ChangeTracker.Entries()
            .Where(IsAuditable)
            .Select(entry => CreateAuditEntry(entry, mesContext.CurrentTenantId, mesContext.AuditRequestContext))
            .ToList();

        if (auditEntries.Count > 0)
        {
            context.Set<DataChangeLog>().AddRange(auditEntries);
        }
    }

    private static bool IsAuditable(EntityEntry entry)
    {
        return entry.Entity is ITenantEntity and not AuditLog and not DataChangeLog and not AgentAuditLog &&
               entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;
    }

    private DataChangeLog CreateAuditEntry(EntityEntry entry, string tenantId, AuditRequestContext? requestContext)
    {
        Dictionary<string, object?> oldValues = new(StringComparer.Ordinal);
        Dictionary<string, object?> newValues = new(StringComparer.Ordinal);

        foreach (PropertyEntry property in entry.Properties)
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                oldValues[property.Metadata.Name] = property.OriginalValue;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                newValues[property.Metadata.Name] = property.CurrentValue;
            }
        }

        return new DataChangeLog
        {
            TenantId = tenantId,
            UserId = requestContext?.UserId,
            EntityType = entry.Metadata.ClrType.Name,
            EntityId = GetEntityId(entry),
            ChangeType = entry.State.ToString(),
            OldValues = oldValues.Count == 0 ? null : _redactor.RedactValues(oldValues),
            NewValues = _redactor.RedactValues(newValues),
            CorrelationId = requestContext?.CorrelationId
        };
    }

    private static string GetEntityId(EntityEntry entry)
    {
        IReadOnlyList<PropertyEntry> keyProperties = entry.Properties
            .Where(property => property.Metadata.IsPrimaryKey())
            .ToList();

        return keyProperties.Count == 0
            ? string.Empty
            : string.Join(",", keyProperties.Select(property => property.CurrentValue?.ToString() ?? string.Empty));
    }
}
