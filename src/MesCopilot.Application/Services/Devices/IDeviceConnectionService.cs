using MesCopilot.Application.Dtos.Devices;

namespace MesCopilot.Application.Services.Devices;

public interface IDeviceConnectionService
{
    Task<IReadOnlyList<DeviceConnectionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DeviceConnectionDto> CreateAsync(DeviceConnectionInput input, CancellationToken cancellationToken = default);
    Task<DeviceConnectionDto?> UpdateAsync(int id, DeviceConnectionInput input, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> SetEnabledAsync(int id, bool enabled, CancellationToken cancellationToken = default);
    Task<bool> TestAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceConnectionRuntime>> GetEnabledRuntimeAsync(CancellationToken cancellationToken = default);
    Task<DeviceConnectionRuntime?> GetRuntimeAsync(int id, CancellationToken cancellationToken = default);
    Task RecordConnectionResultAsync(int id, bool connected, string? errorCode, CancellationToken cancellationToken = default);
}
