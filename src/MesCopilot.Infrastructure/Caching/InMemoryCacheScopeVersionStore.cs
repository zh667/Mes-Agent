using System.Collections.Concurrent;

namespace MesCopilot.Infrastructure.Caching;

public sealed class InMemoryCacheScopeVersionStore : ICacheScopeVersionStore
{
    private readonly ConcurrentDictionary<string, long> _versions = new(StringComparer.Ordinal);

    public Task<string> GetVersionAsync(string tenantId, string scope, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_versions.GetOrAdd(BuildKey(tenantId, scope), 0).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public Task InvalidateAsync(string tenantId, string scope, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _versions.AddOrUpdate(BuildKey(tenantId, scope), 1, (_, version) => version + 1);
        return Task.CompletedTask;
    }

    private static string BuildKey(string tenantId, string scope) => $"{tenantId.Trim()}:{scope.Trim()}";
}
