using MesCopilot.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace MesCopilot.Domain.Entities.Identity;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Operator;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }
}
