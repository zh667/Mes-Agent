using MesCopilot.Domain.Common;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MesCopilot.Infrastructure.Data.Interceptors;

public sealed class TenantWriteGuardInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext? _tenantContext;

    public TenantWriteGuardInterceptor()
    {
    }

    public TenantWriteGuardInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        GuardTenantWrites(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        GuardTenantWrites(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void GuardTenantWrites(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        bool autoDetectChanges = context.ChangeTracker.AutoDetectChangesEnabled;
        context.ChangeTracker.AutoDetectChangesEnabled = false;
        List<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry> tenantEntries;
        try
        {
            tenantEntries = context.ChangeTracker.Entries()
                .Where(entry => entry.Entity is ITenantEntity)
                .ToList();

            foreach (var entry in tenantEntries.Where(entry => entry.State != EntityState.Added))
            {
                ITenantEntity tenantEntity = (ITenantEntity)entry.Entity;
                string originalTenantId = entry.Property(nameof(ITenantEntity.TenantId)).OriginalValue?.ToString() ?? string.Empty;
                if (!TenantIdsMatch(originalTenantId, tenantEntity.TenantId))
                {
                    throw CreateViolation(entry.Metadata.ClrType.Name);
                }
            }
        }
        finally
        {
            context.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
        }

        context.ChangeTracker.DetectChanges();

        foreach (var entry in tenantEntries
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            ITenantEntity tenantEntity = (ITenantEntity)entry.Entity;
            string tenantId = RequireTenantId(context);

            if (entry.State == EntityState.Added)
            {
                if (string.IsNullOrWhiteSpace(tenantEntity.TenantId))
                {
                    tenantEntity.TenantId = tenantId;
                }
                else if (!TenantIdsMatch(tenantEntity.TenantId, tenantId))
                {
                    throw CreateViolation(entry.Metadata.ClrType.Name);
                }

                continue;
            }

            string originalTenantId = entry.Property(nameof(ITenantEntity.TenantId)).OriginalValue?.ToString() ?? string.Empty;
            if (!TenantIdsMatch(originalTenantId, tenantId) ||
                !TenantIdsMatch(tenantEntity.TenantId, tenantId))
            {
                throw CreateViolation(entry.Metadata.ClrType.Name);
            }
        }
    }

    private string RequireTenantId(DbContext context)
    {
        string? tenantId = _tenantContext?.TenantId ?? (context as MesDbContext)?.CurrentTenantId;
        return string.IsNullOrWhiteSpace(tenantId)
            ? throw new TenantBoundaryViolationException("A tenant context is required for business data writes.")
            : tenantId;
    }

    private static bool TenantIdsMatch(string left, string right)
    {
        return string.Equals(left, right, StringComparison.Ordinal);
    }

    private static TenantBoundaryViolationException CreateViolation(string entityName)
    {
        return new TenantBoundaryViolationException($"Tenant boundary violation for entity '{entityName}'.");
    }
}
