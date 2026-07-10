namespace MesCopilot.Infrastructure.Caching;

public interface ICacheScopeVersionStore
{
    Task<string> GetVersionAsync(string tenantId, string scope, CancellationToken cancellationToken = default);

    Task InvalidateAsync(string tenantId, string scope, CancellationToken cancellationToken = default);
}
