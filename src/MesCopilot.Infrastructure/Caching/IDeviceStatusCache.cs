namespace MesCopilot.Infrastructure.Caching;

public interface IDeviceStatusCache
{
    Task<T?> GetAsync<T>(string tenantId, int equipmentId, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string tenantId, int equipmentId, T value, CancellationToken cancellationToken = default);
}
