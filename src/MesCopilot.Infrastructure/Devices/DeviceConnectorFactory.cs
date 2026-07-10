using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Devices.Mqtt;
using MesCopilot.Infrastructure.Devices.Modbus;
using MesCopilot.Infrastructure.Devices.OpcUa;

namespace MesCopilot.Infrastructure.Devices;

public sealed class DeviceConnectorFactory : IDeviceConnectorFactory
{
    private readonly bool _allowInsecureTransport;

    public DeviceConnectorFactory(bool allowInsecureTransport = false)
    {
        _allowInsecureTransport = allowInsecureTransport;
    }

    public IDeviceConnector Create(DeviceProtocol protocol, DeviceConnectionSettings settings, int equipmentId)
    {
        ValidateSettings(settings);
        ValidateProtocolSecurity(protocol, settings);
        return protocol switch
        {
            DeviceProtocol.Mqtt => new MqttDeviceConnector(settings, equipmentId),
            DeviceProtocol.OpcUa => new OpcUaDeviceConnector(settings, equipmentId),
            DeviceProtocol.ModbusTcp => new ModbusTcpDeviceConnector(settings, equipmentId),
            _ => throw new NotSupportedException($"Protocol {protocol} is not supported.")
        };
    }

    private void ValidateProtocolSecurity(DeviceProtocol protocol, DeviceConnectionSettings settings)
    {
        if (protocol == DeviceProtocol.Mqtt &&
            (!string.IsNullOrWhiteSpace(settings.Username) || !string.IsNullOrWhiteSpace(settings.Password)) &&
            !settings.UseTls)
        {
            throw new ArgumentException("MQTT credentials require TLS.", nameof(settings));
        }

        if (protocol == DeviceProtocol.OpcUa &&
            settings.AllowInsecure &&
            !_allowInsecureTransport)
        {
            throw new ArgumentException("Insecure OPC UA transport is disabled in this environment.", nameof(settings));
        }

        if (protocol == DeviceProtocol.OpcUa &&
            !settings.AllowInsecure &&
            string.IsNullOrWhiteSpace(settings.CertificateThumbprint))
        {
            throw new ArgumentException("Secure OPC UA requires a trusted certificate thumbprint.", nameof(settings));
        }

        if (protocol == DeviceProtocol.ModbusTcp &&
            (int)settings.RegisterAddress + settings.RegisterCount > ushort.MaxValue + 1)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "The Modbus register range exceeds 65535.");
        }
    }

    private static void ValidateSettings(DeviceConnectionSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Host);
        if (settings.Port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(settings.Port));
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Endpoint);
    }
}
