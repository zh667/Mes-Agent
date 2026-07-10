using Microsoft.AspNetCore.Identity;

namespace MesCopilot.Domain.Entities.Identity;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public bool IsPlatformAdmin { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public ICollection<UserTenantMembership> TenantMemberships { get; set; } = new List<UserTenantMembership>();
}
