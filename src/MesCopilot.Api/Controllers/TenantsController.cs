using System.Security.Claims;
using MesCopilot.Api.Dtos.Tenants;
using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesCopilot.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ITenantContext _tenantContext;

    public TenantsController(ITenantService tenantService, ITenantContext tenantContext)
    {
        _tenantService = tenantService;
        _tenantContext = tenantContext;
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<TenantSummaryDto>>> GetMine(CancellationToken cancellationToken)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        IReadOnlyList<UserTenantMembership> memberships = await _tenantService.GetActiveMembershipsAsync(userId, cancellationToken);
        return Ok(memberships.Select(ToSummary).ToList());
    }

    [HttpGet("current")]
    public async Task<ActionResult<TenantSummaryDto>> GetCurrent(CancellationToken cancellationToken)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(_tenantContext.TenantId))
        {
            return Unauthorized();
        }

        UserTenantMembership? membership = await _tenantService.FindActiveMembershipAsync(
            userId,
            _tenantContext.TenantId,
            cancellationToken);
        return membership is null ? NotFound() : Ok(ToSummary(membership));
    }

    [HttpPost("switch/{tenantId}")]
    public async Task<ActionResult<TenantSummaryDto>> Switch(string tenantId, CancellationToken cancellationToken)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        UserTenantMembership? membership = await _tenantService.FindActiveMembershipAsync(userId, tenantId, cancellationToken);
        return membership is null
            ? StatusCode(StatusCodes.Status403Forbidden, new TenantProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Code = "TENANT_FORBIDDEN",
                Title = "The selected tenant is not available to this user."
            })
            : Ok(ToSummary(membership));
    }

    [HttpGet]
    [Authorize(Policy = "RequirePlatformAdmin")]
    public async Task<ActionResult<IReadOnlyList<TenantSummaryDto>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<Tenant> tenants = await _tenantService.GetAllAsync(cancellationToken);
        return Ok(tenants.Select(tenant => ToSummary(tenant, "PlatformAdmin")).ToList());
    }

    [HttpPost]
    [Authorize(Policy = "RequirePlatformAdmin")]
    public async Task<ActionResult<TenantSummaryDto>> Create(
        CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        Tenant tenant = await _tenantService.CreateAsync(request.Code, request.Name, cancellationToken);
        return CreatedAtAction(nameof(GetAll), ToSummary(tenant, "PlatformAdmin"));
    }

    [HttpPut("{tenantId}")]
    [Authorize(Policy = "RequirePlatformAdmin")]
    public async Task<ActionResult<TenantSummaryDto>> Update(
        string tenantId,
        UpdateTenantRequest request,
        CancellationToken cancellationToken)
    {
        Tenant? tenant = await _tenantService.UpdateAsync(tenantId, request.Name, request.IsActive, cancellationToken);
        return tenant is null ? NotFound() : Ok(ToSummary(tenant, "PlatformAdmin"));
    }

    [HttpDelete("{tenantId}")]
    [Authorize(Policy = "RequirePlatformAdmin")]
    public async Task<IActionResult> Deactivate(string tenantId, CancellationToken cancellationToken)
    {
        return await _tenantService.DeactivateAsync(tenantId, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpGet("{tenantId}/members")]
    [Authorize(Policy = "RequirePlatformAdmin")]
    public async Task<ActionResult<IReadOnlyList<TenantMemberDto>>> GetMembers(
        string tenantId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<UserTenantMembership> memberships = await _tenantService.GetMembersAsync(tenantId, cancellationToken);
        return Ok(memberships.Select(ToMember).ToList());
    }

    [HttpPost("{tenantId}/members")]
    [Authorize(Policy = "RequirePlatformAdmin")]
    public async Task<ActionResult<TenantMemberDto>> AddMember(
        string tenantId,
        AddTenantMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(request.Role, ignoreCase: true, out UserRole role))
        {
            return BadRequest(new { code = "TENANT_ROLE_INVALID" });
        }

        await _tenantService.AddMemberAsync(tenantId, request.Email, role, cancellationToken);
        UserTenantMembership membership = (await _tenantService.GetMembersAsync(tenantId, cancellationToken))
            .Single(item => string.Equals(item.User.Email, request.Email, StringComparison.OrdinalIgnoreCase));
        return Ok(ToMember(membership));
    }

    [HttpPut("{tenantId}/members/{userId}")]
    [Authorize(Policy = "RequirePlatformAdmin")]
    public async Task<ActionResult<TenantMemberDto>> UpdateMember(
        string tenantId,
        string userId,
        UpdateTenantMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(request.Role, ignoreCase: true, out UserRole role))
        {
            return BadRequest(new { code = "TENANT_ROLE_INVALID" });
        }

        UserTenantMembership? membership = await _tenantService.UpdateMemberAsync(
            tenantId,
            userId,
            role,
            request.IsActive,
            cancellationToken);
        if (membership is null)
        {
            return NotFound();
        }

        UserTenantMembership hydrated = (await _tenantService.GetMembersAsync(tenantId, cancellationToken))
            .Single(item => item.UserId == userId);
        return Ok(ToMember(hydrated));
    }

    private static TenantSummaryDto ToSummary(UserTenantMembership membership)
    {
        return ToSummary(membership.Tenant, membership.Role.ToString());
    }

    private static TenantSummaryDto ToSummary(Tenant tenant, string role)
    {
        return new TenantSummaryDto(tenant.Id, tenant.Code, tenant.Name, role, tenant.IsActive);
    }

    private static TenantMemberDto ToMember(UserTenantMembership membership)
    {
        return new TenantMemberDto(
            membership.UserId,
            membership.User.Email ?? string.Empty,
            membership.User.DisplayName,
            membership.Role.ToString(),
            membership.IsActive);
    }
}
