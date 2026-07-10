using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace MesCopilot.Infrastructure.Caching;

public sealed class DistributedCacheScopeVersionStore : ICacheScopeVersionStore
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheScopeVersionStore> _logger;

    public DistributedCacheScopeVersionStore(
        IDistributedCache cache,
        ILogger<DistributedCacheScopeVersionStore> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GetVersionAsync(string tenantId, string scope, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _cache.GetStringAsync(BuildKey(tenantId, scope), cancellationToken) ?? "0";
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Cache scope version lookup failed for tenant {TenantId} and scope {Scope}.", tenantId, scope);
            return Guid.NewGuid().ToString("N");
        }
    }

    public async Task InvalidateAsync(string tenantId, string scope, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.SetStringAsync(
                BuildKey(tenantId, scope),
                Guid.NewGuid().ToString("N"),
                new DistributedCacheEntryOptions(),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Cache scope invalidation failed for tenant {TenantId} and scope {Scope}.", tenantId, scope);
        }
    }

    private static string BuildKey(string tenantId, string scope) => $"mes:scope:{tenantId.Trim()}:{scope.Trim()}";
}
