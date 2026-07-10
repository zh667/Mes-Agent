using System.Security.Claims;
using MesCopilot.Api.Dtos.Tenants;
using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Infrastructure.Tenancy;

namespace MesCopilot.Api.Middleware;

public sealed class TenantResolutionMiddleware
{
    public const string HeaderName = "X-Tenant-Id";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantService tenantService,
        CurrentTenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated != true || IsTenantOptionalRequest(context))
        {
            await _next(context);
            return;
        }

        bool isPlatformAdmin = string.Equals(
            context.User.FindFirstValue(MesCopilotClaimTypes.PlatformAdmin),
            bool.TrueString,
            StringComparison.OrdinalIgnoreCase);

        string? tenantSelection = GetTenantSelection(context);
        if (string.IsNullOrWhiteSpace(tenantSelection))
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "TENANT_REQUIRED", "Select an active tenant.");
            return;
        }

        string tenantId = tenantSelection.Trim();
        string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        UserTenantMembership? membership = string.IsNullOrWhiteSpace(userId)
            ? null
            : await tenantService.FindActiveMembershipAsync(userId, tenantId, context.RequestAborted);
        if (membership is null)
        {
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "TENANT_FORBIDDEN", "The selected tenant is not available to this user.");
            return;
        }

        if (context.User.Identity is ClaimsIdentity identity)
        {
            foreach (Claim roleClaim in identity.FindAll(ClaimTypes.Role).ToList())
            {
                identity.RemoveClaim(roleClaim);
            }

            identity.AddClaim(new Claim(ClaimTypes.Role, membership.Role.ToString()));
        }

        tenantContext.Initialize(new TenantResolution(membership.TenantId, isPlatformAdmin));
        await _next(context);
    }

    private static string? GetTenantSelection(HttpContext context)
    {
        string?[] tenantHeaders = context.Request.Headers[HeaderName].ToArray();
        if (tenantHeaders.Length == 1 && !string.IsNullOrWhiteSpace(tenantHeaders[0]))
        {
            return tenantHeaders[0];
        }

        if (context.Request.Path.StartsWithSegments("/hubs/equipment"))
        {
            string?[] tenantQuery = context.Request.Query["tenantId"].ToArray();
            return tenantQuery.Length == 1 ? tenantQuery[0] : null;
        }

        return null;
    }

    private static bool IsTenantOptionalRequest(HttpContext context)
    {
        PathString path = context.Request.Path;
        return path.StartsWithSegments("/health") ||
            path.StartsWithSegments("/swagger") ||
            path.StartsWithSegments("/api/auth/login") ||
            path.StartsWithSegments("/api/auth/register") ||
            path.StartsWithSegments("/api/auth/refresh") ||
            path.StartsWithSegments("/api/auth/me") ||
            path.StartsWithSegments("/api/auth/logout") ||
            (path.StartsWithSegments("/api/tenants") &&
                !path.StartsWithSegments("/api/tenants/current"));
    }

    private static Task WriteProblemAsync(HttpContext context, int status, string code, string title)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(new TenantProblemDetails
        {
            Status = status,
            Code = code,
            Title = title
        });
    }
}
