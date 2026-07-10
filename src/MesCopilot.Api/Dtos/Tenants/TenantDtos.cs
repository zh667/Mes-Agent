using System.ComponentModel.DataAnnotations;

namespace MesCopilot.Api.Dtos.Tenants;

public sealed record TenantSummaryDto(
    string Id,
    string Code,
    string Name,
    string Role,
    bool IsActive);

public sealed record TenantMemberDto(
    string UserId,
    string Email,
    string DisplayName,
    string Role,
    bool IsActive);

public sealed record CreateTenantRequest(
    [Required, MaxLength(50)] string Code,
    [Required, MaxLength(200)] string Name);

public sealed record UpdateTenantRequest(
    [Required, MaxLength(200)] string Name,
    bool IsActive);

public sealed record AddTenantMemberRequest(
    [Required, EmailAddress] string Email,
    [Required] string Role);

public sealed record UpdateTenantMemberRequest(
    [Required] string Role,
    bool IsActive);

public sealed class TenantProblemDetails
{
    public string Code { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public int Status { get; init; }
}
