using Microsoft.Extensions.Caching.Distributed;

namespace MesCopilot.Infrastructure.Identity;

public sealed class RedisAccessTokenRevocationStore : IAccessTokenRevocationStore
{
    private readonly IDistributedCache _cache;

    public RedisAccessTokenRevocationStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task RevokeAsync(string tokenId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenId);
        TimeSpan remaining = expiresAt - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            return;
        }

        await _cache.SetStringAsync(
            BuildKey(tokenId),
            "revoked",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = remaining },
            cancellationToken);
    }

    public async Task<bool> IsRevokedAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        return !string.IsNullOrWhiteSpace(tokenId) &&
               await _cache.GetStringAsync(BuildKey(tokenId), cancellationToken) is not null;
    }

    private static string BuildKey(string tokenId) => $"mes:jwt:revoked:{tokenId}";
}
