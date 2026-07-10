using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesCopilot.Agent.Models;
using MesCopilot.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace MesCopilot.Agent.Caching;

public sealed class RedisAgentResponseCache : IAgentResponseCache
{
    private readonly IDistributedCache _cache;
    private readonly ICacheScopeVersionStore _scopeVersions;
    private readonly ILogger<RedisAgentResponseCache> _logger;

    public RedisAgentResponseCache(
        IDistributedCache cache,
        ICacheScopeVersionStore scopeVersions,
        ILogger<RedisAgentResponseCache> logger)
    {
        _cache = cache;
        _scopeVersions = scopeVersions;
        _logger = logger;
    }

    public async Task<FunctionCallResult?> GetAsync(
        string tenantId, string scope, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            string cacheKey = await BuildKeyAsync(tenantId, scope, key, cancellationToken);
            string? json = await _cache.GetStringAsync(cacheKey, cancellationToken);
            return json is null ? null : JsonSerializer.Deserialize<FunctionCallResult>(json);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Agent cache read failed for tenant {TenantId} and scope {Scope}.", tenantId, scope);
            return null;
        }
    }

    public async Task SetAsync(
        string tenantId, string scope, string key, FunctionCallResult value, TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string cacheKey = await BuildKeyAsync(tenantId, scope, key, cancellationToken);
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(value),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Agent cache write failed for tenant {TenantId} and scope {Scope}.", tenantId, scope);
        }
    }

    public Task InvalidateScopeAsync(string tenantId, string scope, CancellationToken cancellationToken = default)
    {
        return _scopeVersions.InvalidateAsync(tenantId, scope, cancellationToken);
    }

    private async Task<string> BuildKeyAsync(
        string tenantId, string scope, string key, CancellationToken cancellationToken)
    {
        string version = await _scopeVersions.GetVersionAsync(tenantId, scope, cancellationToken);
        string digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key.Trim())));
        return $"mes:agent:{tenantId.Trim()}:{scope.Trim()}:{version}:{digest}";
    }
}
