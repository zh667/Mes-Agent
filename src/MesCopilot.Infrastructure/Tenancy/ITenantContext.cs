namespace MesCopilot.Infrastructure.Tenancy;

public interface ITenantContext
{
    string? TenantId { get; }

    bool IsPlatformAdmin { get; }
}

public sealed record TenantResolution(string TenantId, bool IsPlatformAdmin);
