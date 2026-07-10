using MesCopilot.Domain.Enums;

namespace MesCopilot.Infrastructure.Devices;

public sealed record DeviceConnectionSettings(
    string Host,
    int Port,
    string Endpoint,
    string? Username = null,
    string? Password = null,
    string? CertificateThumbprint = null,
    byte UnitId = 1,
    ushort RegisterAddress = 0,
    ushort RegisterCount = 1,
    int PollIntervalMilliseconds = 1000,
    bool UseTls = false,
    bool AllowInsecure = false);

public sealed record DeviceReading(
    int EquipmentId,
    string State,
    DateTime Timestamp,
    IReadOnlyDictionary<string, decimal>? Metrics = null);

public interface IDeviceConnector : IAsyncDisposable
{
    DeviceProtocol Protocol { get; }
    Task ConnectAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<DeviceReading> SubscribeAsync(CancellationToken cancellationToken);
    Task DisconnectAsync(CancellationToken cancellationToken);
}

public interface IDeviceConnectorFactory
{
    IDeviceConnector Create(DeviceProtocol protocol, DeviceConnectionSettings settings, int equipmentId);
}
