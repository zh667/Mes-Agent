using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace MesCopilot.Infrastructure.Caching;

public sealed class DistributedCacheHealthProbe : ICacheHealthProbe
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheHealthProbe> _logger;

    public DistributedCacheHealthProbe(IDistributedCache cache, ILogger<DistributedCacheHealthProbe> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        string key = $"mes:health:{Guid.NewGuid():N}";
        try
        {
            await _cache.SetStringAsync(
                key,
                "ok",
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10) },
                cancellationToken);
            string? value = await _cache.GetStringAsync(key, cancellationToken);
            await _cache.RemoveAsync(key, cancellationToken);
            return value == "ok";
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Distributed cache health check failed.");
            return false;
        }
    }
}
