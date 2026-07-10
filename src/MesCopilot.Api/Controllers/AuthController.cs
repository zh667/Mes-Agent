using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MesCopilot.Api.Dtos.Auth;
using MesCopilot.Api.Dtos.Tenants;
using MesCopilot.Api.Errors;
using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Identity;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace MesCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ITenantService _tenantService;
    private readonly IConfiguration _configuration;
    private readonly IAccessTokenRevocationStore _revocationStore;
    private readonly ApiProblemFactory _problems;

    public AuthController(
        UserManager<AppUser> userManager,
        ITokenService tokenService,
        ITenantService tenantService,
        IConfiguration configuration,
        IAccessTokenRevocationStore revocationStore,
        ApiProblemFactory problems)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _tenantService = tenantService;
        _configuration = configuration;
        _revocationStore = revocationStore;
        _problems = problems;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!_configuration.GetValue("Auth:AllowRegistration", false))
        {
            return StatusCode(StatusCodes.Status403Forbidden, _problems.Create(
                StatusCodes.Status403Forbidden,
                "REGISTRATION_DISABLED",
                "ForbiddenTitle",
                "RegistrationDisabled"));
        }

        AppUser? existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Conflict(_problems.Create(
                StatusCodes.Status409Conflict,
                "EMAIL_ALREADY_REGISTERED",
                "ConflictTitle",
                "EmailAlreadyRegistered"));
        }

        AppUser user = new()
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            IsPlatformAdmin = false,
            CreatedAt = DateTime.UtcNow
        };

        IdentityResult result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(_problems.Create(
                StatusCodes.Status400BadRequest,
                "REGISTRATION_FAILED",
                "ValidationTitle",
                "RegistrationFailed"));
        }

        await _tenantService.AddMemberAsync(
            SeedData.DefaultTenantId,
            request.Email,
            UserRole.Operator,
            HttpContext.RequestAborted);

        AuthResponse response = await IssueTokensAsync(user);
        return CreatedAtAction(nameof(Me), response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        AppUser? user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(_problems.Create(
                StatusCodes.Status401Unauthorized,
                "INVALID_CREDENTIALS",
                "AuthenticationTitle",
                "InvalidCredentials"));
        }

        bool passwordMatches = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordMatches)
        {
            return Unauthorized(_problems.Create(
                StatusCodes.Status401Unauthorized,
                "INVALID_CREDENTIALS",
                "AuthenticationTitle",
                "InvalidCredentials"));
        }

        user.LastLoginAt = DateTime.UtcNow;
        return Ok(await IssueTokensAsync(user));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
    {
        ClaimsPrincipal? principal;
        try
        {
            principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        }
        catch (Exception ex) when (ex is ArgumentException or SecurityTokenException)
        {
            return Unauthorized(_problems.Create(
                StatusCodes.Status401Unauthorized,
                "INVALID_TOKEN",
                "AuthenticationTitle",
                "InvalidToken"));
        }

        string? userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(_problems.Create(
                StatusCodes.Status401Unauthorized,
                "INVALID_TOKEN",
                "AuthenticationTitle",
                "InvalidToken"));
        }

        AppUser? user = await _userManager.FindByIdAsync(userId);
        string refreshTokenHash = _tokenService.HashRefreshToken(request.RefreshToken);

        if (user is null ||
            user.RefreshToken != refreshTokenHash ||
            user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Unauthorized(_problems.Create(
                StatusCodes.Status401Unauthorized,
                "REFRESH_TOKEN_INVALID_OR_EXPIRED",
                "AuthenticationTitle",
                "RefreshTokenInvalidOrExpired"));
        }

        // TODO Phase 2 hardening: rotate refresh tokens with compare-and-swap to avoid concurrent refresh races.
        return Ok(await IssueTokensAsync(user));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        AppUser? user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _userManager.UpdateAsync(user);

        string? tokenId = User.FindFirstValue(JwtRegisteredClaimNames.Jti) ?? User.FindFirstValue("jti");
        string? expirationClaim = User.FindFirstValue(JwtRegisteredClaimNames.Exp) ?? User.FindFirstValue("exp");
        if (!string.IsNullOrWhiteSpace(tokenId) &&
            long.TryParse(expirationClaim, out long expirationSeconds))
        {
            await _revocationStore.RevokeAsync(
                tokenId,
                DateTimeOffset.FromUnixTimeSeconds(expirationSeconds),
                HttpContext.RequestAborted);
        }

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> Me()
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        AppUser? user = await _userManager.FindByIdAsync(userId);
        return user is null ? NotFound() : Ok(await ToUserInfoAsync(user));
    }

    private async Task<AuthResponse> IssueTokensAsync(AppUser user)
    {
        string accessToken = _tokenService.GenerateAccessToken(user);
        string refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = _tokenService.HashRefreshToken(refreshToken);
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = await ToUserInfoAsync(user)
        };
    }

    private async Task<UserInfo> ToUserInfoAsync(AppUser user)
    {
        IReadOnlyList<UserTenantMembership> memberships = await _tenantService.GetActiveMembershipsAsync(
            user.Id,
            HttpContext.RequestAborted);
        return new UserInfo
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            IsPlatformAdmin = user.IsPlatformAdmin,
            Tenants = memberships.Select(membership => new TenantSummaryDto(
                membership.TenantId,
                membership.Tenant.Code,
                membership.Tenant.Name,
                membership.Role.ToString(),
                membership.Tenant.IsActive)).ToList()
        };
    }
}
