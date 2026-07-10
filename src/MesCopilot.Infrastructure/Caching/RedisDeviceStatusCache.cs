using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace MesCopilot.Infrastructure.Caching;

public sealed class RedisDeviceStatusCache : IDeviceStatusCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(10);
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisDeviceStatusCache> _logger;

    public RedisDeviceStatusCache(IDistributedCache cache, ILogger<RedisDeviceStatusCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string tenantId, int equipmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            string? json = await _cache.GetStringAsync(BuildKey(tenantId, equipmentId), cancellationToken);
            return json is null ? default : JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Device status cache read failed for equipment {EquipmentId}.", equipmentId);
            return default;
        }
    }

    public async Task SetAsync<T>(string tenantId, int equipmentId, T value, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.SetStringAsync(
                BuildKey(tenantId, equipmentId),
                JsonSerializer.Serialize(value),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl },
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Device status cache write failed for equipment {EquipmentId}.", equipmentId);
        }
    }

    private static string BuildKey(string tenantId, int equipmentId) => $"mes:device:{tenantId.Trim()}:{equipmentId}";
}
