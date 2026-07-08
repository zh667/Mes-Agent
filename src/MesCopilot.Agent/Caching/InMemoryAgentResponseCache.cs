using System.Collections.Concurrent;
using MesCopilot.Agent.Models;

namespace MesCopilot.Agent.Caching;

public class InMemoryAgentResponseCache : IAgentResponseCache
{
    private const int MaxEntries = 1000;

    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<string> _insertionOrder = new();

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
        _insertionOrder.Enqueue(key);
        TrimToLimit();
    }

    private void TrimToLimit()
    {
        while (_entries.Count > MaxEntries && _insertionOrder.TryDequeue(out string? oldestKey))
        {
            _entries.TryRemove(oldestKey, out _);
        }
    }

    private sealed record CacheEntry(FunctionCallResult Result, DateTimeOffset ExpiresAtUtc);
}
