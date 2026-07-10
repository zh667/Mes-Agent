using System.Net;
using System.Net.Sockets;
using MesCopilot.DeviceSimulator.Workers;
using MesCopilot.Infrastructure.Devices;
using MesCopilot.Infrastructure.Devices.Modbus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesCopilot.IntegrationTests.Devices;

public class ModbusTcpDeviceConnectorTests
{
    [Fact]
    public async Task Connector_ReadsSimulatorHoldingRegisterWithoutWriting()
    {
        int port = GetFreePort();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Modbus:Port"] = port.ToString(),
            ["Modbus:UnitId"] = "1"
        }).Build();
        ModbusEquipmentServer server = new(configuration, NullLogger<ModbusEquipmentServer>.Instance);
        await server.StartAsync(default);
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        await server.WaitUntilStartedAsync(timeout.Token);
        await using ModbusTcpDeviceConnector connector = new(
            new DeviceConnectionSettings("127.0.0.1", port, "holding", UnitId: 1, RegisterAddress: 0, RegisterCount: 3, PollIntervalMilliseconds: 100),
            7);
        try
        {
            await connector.ConnectAsync(timeout.Token);
            await using IAsyncEnumerator<DeviceReading> readings = connector.SubscribeAsync(timeout.Token).GetAsyncEnumerator(timeout.Token);
            Assert.True(await readings.MoveNextAsync());
            Assert.Equal("Running", readings.Current.State);
            Assert.Equal(125m, readings.Current.Metrics!["register1"]);
        }
        finally
        {
            await connector.DisconnectAsync(default);
            await server.StopAsync(default);
        }
    }

    private static int GetFreePort()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
