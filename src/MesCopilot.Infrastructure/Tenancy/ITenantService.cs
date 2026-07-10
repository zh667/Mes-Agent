using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Domain.Enums;

namespace MesCopilot.Infrastructure.Tenancy;

public interface ITenantService
{
    Task<UserTenantMembership?> FindActiveMembershipAsync(
        string userId,
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserTenantMembership>> GetActiveMembershipsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Tenant?> GetByIdAsync(string tenantId, CancellationToken cancellationToken = default);

    Task<Tenant> CreateAsync(string code, string name, CancellationToken cancellationToken = default);

    Task<Tenant?> UpdateAsync(
        string tenantId,
        string name,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(string tenantId, CancellationToken cancellationToken = default);

    Task<UserTenantMembership> AddMemberAsync(
        string tenantId,
        string email,
        UserRole role,
        CancellationToken cancellationToken = default);

    Task<UserTenantMembership?> UpdateMemberAsync(
        string tenantId,
        string userId,
        UserRole role,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserTenantMembership>> GetMembersAsync(
        string tenantId,
        CancellationToken cancellationToken = default);
}
