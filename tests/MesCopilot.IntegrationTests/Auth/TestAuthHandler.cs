using System.Security.Claims;
using System.Text.Encodings.Web;
using MesCopilot.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesCopilot.IntegrationTests.Auth;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestScheme";
    public const string DefaultUserId = "test-user-id";
    public const string DefaultEmail = "test@example.com";
    public const string DefaultRole = "Admin";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, DefaultUserId),
            new Claim(ClaimTypes.Email, DefaultEmail),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(
                MesCopilotClaimTypes.PlatformAdmin,
                Request.Headers["X-Test-Platform-Admin"].SingleOrDefault() ?? bool.FalseString.ToLowerInvariant())
        ];

        ClaimsIdentity identity = new(claims, AuthenticationScheme);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
