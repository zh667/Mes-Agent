using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;

namespace MesCopilot.UnitTests.Infrastructure.Identity;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "ThisIsATestSecretKeyThatIsAtLeast32BytesLong!!",
                ["Jwt:Issuer"] = "MesCopilot",
                ["Jwt:Audience"] = "MesCopilotClient",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            })
            .Build();

        _tokenService = new TokenService(configuration);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwtWithUserClaims()
    {
        AppUser user = CreateUser("user-123", "test@example.com", "Test User", isPlatformAdmin: true);

        string token = _tokenService.GenerateAccessToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
        var handler = new JwtSecurityTokenHandler();
        JwtSecurityToken jwt = handler.ReadJwtToken(token);

        Assert.Equal("user-123", jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("test@example.com", jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("Test User", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.DoesNotContain(jwt.Claims, claim => claim.Type == ClaimTypes.Role);
        Assert.Equal("true", jwt.Claims.First(c => c.Type == MesCopilotClaimTypes.PlatformAdmin).Value);
        Assert.False(string.IsNullOrWhiteSpace(jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value));
    }

    [Fact]
    public void GenerateAccessToken_ShouldUseConfiguredExpiration()
    {
        AppUser user = CreateUser("user-123", "test@example.com", "Test User", isPlatformAdmin: false);

        string token = _tokenService.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        JwtSecurityToken jwt = handler.ReadJwtToken(token);

        DateTime expectedExpiry = DateTime.UtcNow.AddMinutes(15);
        Assert.True(jwt.ValidTo <= expectedExpiry.AddSeconds(5));
        Assert.True(jwt.ValidTo >= expectedExpiry.AddSeconds(-5));
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueOpaqueTokens()
    {
        string token1 = _tokenService.GenerateRefreshToken();
        string token2 = _tokenService.GenerateRefreshToken();

        Assert.False(string.IsNullOrWhiteSpace(token1));
        Assert.False(string.IsNullOrWhiteSpace(token2));
        Assert.True(token1.Length >= 32);
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ShouldReturnClaimsWithoutLifetimeValidation()
    {
        AppUser user = CreateUser("user-456", "expired@example.com", "Expired User", isPlatformAdmin: false);
        string token = _tokenService.GenerateAccessToken(user);

        ClaimsPrincipal? principal = _tokenService.GetPrincipalFromExpiredToken(token);

        Assert.NotNull(principal);
        Assert.Equal("user-456", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Null(principal.FindFirst(ClaimTypes.Role));
    }

    private static AppUser CreateUser(string id, string email, string displayName, bool isPlatformAdmin)
    {
        return new AppUser
        {
            Id = id,
            Email = email,
            UserName = email,
            DisplayName = displayName,
            IsPlatformAdmin = isPlatformAdmin
        };
    }
}
