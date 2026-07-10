using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Infrastructure.Tenancy;

public sealed class TenantService : ITenantService
{
    private readonly MesDbContext _context;

    public TenantService(MesDbContext context)
    {
        _context = context;
    }

    public Task<UserTenantMembership?> FindActiveMembershipAsync(
        string userId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        return _context.UserTenantMemberships
            .AsNoTracking()
            .Include(membership => membership.Tenant)
            .SingleOrDefaultAsync(
                membership => membership.UserId == userId &&
                    membership.TenantId == tenantId &&
                    membership.IsActive &&
                    membership.Tenant.IsActive,
                cancellationToken);
    }

    public async Task<IReadOnlyList<UserTenantMembership>> GetActiveMembershipsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserTenantMemberships
            .AsNoTracking()
            .Include(membership => membership.Tenant)
            .Where(membership => membership.UserId == userId && membership.IsActive && membership.Tenant.IsActive)
            .OrderBy(membership => membership.Tenant.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .AsNoTracking()
            .OrderBy(tenant => tenant.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<Tenant?> GetByIdAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return _context.Tenants.AsNoTracking().SingleOrDefaultAsync(tenant => tenant.Id == tenantId, cancellationToken);
    }

    public async Task<Tenant> CreateAsync(
        string code,
        string name,
        CancellationToken cancellationToken = default)
    {
        Tenant tenant = new()
        {
            Id = Guid.NewGuid().ToString(),
            Code = code,
            Name = name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);
        return tenant;
    }

    public async Task<Tenant?> UpdateAsync(
        string tenantId,
        string name,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        Tenant? tenant = await _context.Tenants.SingleOrDefaultAsync(item => item.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        tenant.Name = name.Trim();
        tenant.IsActive = isActive;
        await _context.SaveChangesAsync(cancellationToken);
        return tenant;
    }

    public async Task<bool> DeactivateAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        Tenant? tenant = await _context.Tenants.SingleOrDefaultAsync(item => item.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            return false;
        }

        tenant.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserTenantMembership> AddMemberAsync(
        string tenantId,
        string email,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        AppUser user = await _context.Users.SingleOrDefaultAsync(
                item => item.NormalizedEmail == email.Trim().ToUpperInvariant(),
                cancellationToken)
            ?? throw new ArgumentException("User was not found.", nameof(email));

        UserTenantMembership? existing = await _context.UserTenantMemberships.SingleOrDefaultAsync(
            membership => membership.UserId == user.Id && membership.TenantId == tenantId,
            cancellationToken);
        if (existing is not null)
        {
            existing.Role = role;
            existing.IsActive = true;
            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        UserTenantMembership membership = new()
        {
            UserId = user.Id,
            TenantId = tenantId,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserTenantMemberships.Add(membership);
        await _context.SaveChangesAsync(cancellationToken);
        return membership;
    }

    public async Task<UserTenantMembership?> UpdateMemberAsync(
        string tenantId,
        string userId,
        UserRole role,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        UserTenantMembership? membership = await _context.UserTenantMemberships
            .SingleOrDefaultAsync(
                item => item.TenantId == tenantId && item.UserId == userId,
                cancellationToken);
        if (membership is null)
        {
            return null;
        }

        membership.Role = role;
        membership.IsActive = isActive;
        await _context.SaveChangesAsync(cancellationToken);
        return membership;
    }

    public async Task<IReadOnlyList<UserTenantMembership>> GetMembersAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserTenantMemberships
            .AsNoTracking()
            .Include(membership => membership.User)
            .Where(membership => membership.TenantId == tenantId)
            .OrderBy(membership => membership.User.DisplayName)
            .ToListAsync(cancellationToken);
    }
}
