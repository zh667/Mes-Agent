namespace MesCopilot.Infrastructure.Tenancy;

public sealed class CurrentTenantContext : ITenantContext
{
    private TenantResolution? _resolution;

    public string? TenantId => _resolution?.TenantId;

    public bool IsPlatformAdmin => _resolution?.IsPlatformAdmin ?? false;

    public void Initialize(TenantResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        if (string.IsNullOrWhiteSpace(resolution.TenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(resolution));
        }

        if (_resolution is not null)
        {
            throw new InvalidOperationException("Tenant context has already been initialized for this scope.");
        }

        _resolution = resolution with { TenantId = resolution.TenantId.Trim() };
    }
}
