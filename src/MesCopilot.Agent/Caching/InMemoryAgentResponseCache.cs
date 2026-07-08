using System.Collections.Concurrent;
using MesCopilot.Agent.Models;

namespace MesCopilot.Agent.Caching;

public class InMemoryAgentResponseCache : IAgentResponseCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);

    public bool TryGet(string key, out FunctionCallResult result)
    {
        if (_entries.TryGetValue(key, out CacheEntry? entry) &&
            entry.ExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            result = entry.Result;
            return true;
        }

        _entries.TryRemove(key, out _);
        result = new FunctionCallResult();
        return false;
    }

    public void Set(string key, FunctionCallResult result, TimeSpan duration)
    {
        _entries[key] = new CacheEntry(result, DateTimeOffset.UtcNow.Add(duration));
    }

    private sealed record CacheEntry(FunctionCallResult Result, DateTimeOffset ExpiresAtUtc);
}
