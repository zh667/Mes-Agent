using System.Security.Claims;
using MesCopilot.Api.Dtos.Auth;
using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Identity;
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

    public AuthController(
        UserManager<AppUser> userManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        AppUser? existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Conflict(new { message = "Email already registered." });
        }

        if (!Enum.TryParse(request.Role, ignoreCase: true, out UserRole role))
        {
            role = UserRole.Operator;
        }

        AppUser user = new()
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        IdentityResult result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(error => error.Description) });
        }

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
            return Unauthorized(new { message = "Invalid credentials." });
        }

        bool passwordMatches = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordMatches)
        {
            return Unauthorized(new { message = "Invalid credentials." });
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
            return Unauthorized(new { message = "Invalid token." });
        }

        string? userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "Invalid token." });
        }

        AppUser? user = await _userManager.FindByIdAsync(userId);
        string refreshTokenHash = _tokenService.HashRefreshToken(request.RefreshToken);

        if (user is null ||
            user.RefreshToken != refreshTokenHash ||
            user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token." });
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
        return user is null ? NotFound() : Ok(ToUserInfo(user));
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
            User = ToUserInfo(user)
        };
    }

    private static UserInfo ToUserInfo(AppUser user)
    {
        return new UserInfo
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString()
        };
    }
}
