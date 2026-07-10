using MesCopilot.Domain.Enums;

namespace MesCopilot.Domain.Entities.Identity;

public class UserTenantMembership
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Operator;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;

    public Tenant Tenant { get; set; } = null!;
}
