using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using MesCopilot.Agent.Models;
using MesCopilot.Infrastructure.Caching;

namespace MesCopilot.Agent.Caching;

public class InMemoryAgentResponseCache : IAgentResponseCache
{
    private const int MaxEntries = 1000;

    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<string> _insertionOrder = new();
    private readonly ICacheScopeVersionStore _scopeVersions;

    public InMemoryAgentResponseCache()
        : this(new InMemoryCacheScopeVersionStore())
    {
    }

    public InMemoryAgentResponseCache(ICacheScopeVersionStore scopeVersions)
    {
        _scopeVersions = scopeVersions;
    }

    public async Task<FunctionCallResult?> GetAsync(
        string tenantId,
        string scope,
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string cacheKey = await BuildCacheKeyAsync(tenantId, scope, key, cancellationToken);
        if (_entries.TryGetValue(cacheKey, out CacheEntry? entry) &&
            entry.ExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            return entry.Result;
        }

        _entries.TryRemove(cacheKey, out _);
        return null;
    }

    public async Task SetAsync(
        string tenantId,
        string scope,
        string key,
        FunctionCallResult value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string cacheKey = await BuildCacheKeyAsync(tenantId, scope, key, cancellationToken);
        _entries[cacheKey] = new CacheEntry(value, DateTimeOffset.UtcNow.Add(ttl));
        _insertionOrder.Enqueue(cacheKey);
        TrimToLimit();
    }

    public Task InvalidateScopeAsync(
        string tenantId,
        string scope,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _scopeVersions.InvalidateAsync(tenantId, scope, cancellationToken);
    }

    private void TrimToLimit()
    {
        while (_entries.Count > MaxEntries && _insertionOrder.TryDequeue(out string? oldestKey))
        {
            _entries.TryRemove(oldestKey, out _);
        }
    }

    private async Task<string> BuildCacheKeyAsync(
        string tenantId,
        string scope,
        string key,
        CancellationToken cancellationToken)
    {
        string scopeKey = BuildScopeKey(tenantId, scope);
        string version = await _scopeVersions.GetVersionAsync(tenantId, scope, cancellationToken);
        string digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key.Trim())));
        return $"{scopeKey}:{version}:{digest}";
    }

    private static string BuildScopeKey(string tenantId, string scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        return $"{tenantId.Trim()}:{scope.Trim()}";
    }

    private sealed record CacheEntry(FunctionCallResult Result, DateTimeOffset ExpiresAtUtc);
}
