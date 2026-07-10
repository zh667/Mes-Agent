using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Devices;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.UnitTests.Infrastructure.Devices;

public class DeviceConnectorFactoryTests
{
    [Fact]
    public async Task Factory_CreatesMqttConnectorForMqttProtocol()
    {
        await using IDeviceConnector connector = new DeviceConnectorFactory().Create(
            DeviceProtocol.Mqtt,
            new DeviceConnectionSettings("localhost", 1883, "mes/equipment/1"),
            1);

        Assert.Equal(DeviceProtocol.Mqtt, connector.Protocol);
    }

    [Fact]
    public void DataProtectionProtector_DoesNotExposePlaintextAndRoundTrips()
    {
        IDataProtectionProvider provider = new EphemeralDataProtectionProvider();
        DataProtectionDeviceConnectionSecretProtector protector = new(provider);

        string encrypted = protector.Protect("mqtt-password");

        Assert.DoesNotContain("mqtt-password", encrypted, StringComparison.Ordinal);
        Assert.Equal("mqtt-password", protector.Unprotect(encrypted));
    }

    [Fact]
    public void Factory_RejectsMqttCredentialsWithoutTls()
    {
        DeviceConnectionSettings settings = new(
            "broker.example.test",
            1883,
            "mes/equipment/1",
            Username: "collector",
            Password: "secret");

        Assert.Throws<ArgumentException>(() =>
            new DeviceConnectorFactory().Create(DeviceProtocol.Mqtt, settings, 1));
    }

    [Fact]
    public void Factory_RequiresExplicitOptInForInsecureOpcUa()
    {
        DeviceConnectionSettings settings = new("opcua.example.test", 4840, "ns=2;s=Equipment/State");

        Assert.Throws<ArgumentException>(() =>
            new DeviceConnectorFactory().Create(DeviceProtocol.OpcUa, settings, 1));
    }

    [Fact]
    public async Task Factory_DevelopmentPolicy_AllowsExplicitInsecureOpcUaForSimulator()
    {
        DeviceConnectionSettings settings = new(
            "localhost",
            4840,
            "ns=2;s=Equipment/State",
            AllowInsecure: true);

        await using IDeviceConnector connector = new DeviceConnectorFactory(allowInsecureTransport: true)
            .Create(DeviceProtocol.OpcUa, settings, 1);

        Assert.Equal(DeviceProtocol.OpcUa, connector.Protocol);
    }

    [Fact]
    public void Factory_RejectsModbusRegisterRangeOverflow()
    {
        DeviceConnectionSettings settings = new(
            "modbus.example.test",
            502,
            "holding",
            UnitId: 1,
            RegisterAddress: ushort.MaxValue,
            RegisterCount: 2);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DeviceConnectorFactory().Create(DeviceProtocol.ModbusTcp, settings, 1));
    }

    [Fact]
    public void DataProtectionProtector_PersistedKeyRingSurvivesProviderRecreation()
    {
        string keyPath = Path.Combine(Path.GetTempPath(), $"mescopilot-keys-{Guid.NewGuid():N}");
        Directory.CreateDirectory(keyPath);
        try
        {
            string encrypted;
            using (ServiceProvider firstServices = CreatePersistedDataProtectionServices(keyPath))
            {
                IDataProtectionProvider firstProvider = firstServices.GetRequiredService<IDataProtectionProvider>();
                encrypted = new DataProtectionDeviceConnectionSecretProtector(firstProvider).Protect("device-secret");
            }

            using ServiceProvider secondServices = CreatePersistedDataProtectionServices(keyPath);
            IDataProtectionProvider secondProvider = secondServices.GetRequiredService<IDataProtectionProvider>();
            string decrypted = new DataProtectionDeviceConnectionSecretProtector(secondProvider).Unprotect(encrypted);

            Assert.Equal("device-secret", decrypted);
        }
        finally
        {
            Directory.Delete(keyPath, recursive: true);
        }
    }

    private static ServiceProvider CreatePersistedDataProtectionServices(string keyPath)
    {
        ServiceCollection services = new();
        services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyPath));
        return services.BuildServiceProvider();
    }
}
