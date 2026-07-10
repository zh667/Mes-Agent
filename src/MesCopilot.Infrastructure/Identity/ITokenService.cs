using System.Security.Claims;
using MesCopilot.Domain.Entities.Identity;

namespace MesCopilot.Infrastructure.Identity;

public interface ITokenService
{
    string GenerateAccessToken(AppUser user);

    string GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);

    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
