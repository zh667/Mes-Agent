using System.Net;
using System.Net.Sockets;
using MesCopilot.DeviceSimulator.Workers;
using MesCopilot.Infrastructure.Devices;
using MesCopilot.Infrastructure.Devices.OpcUa;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesCopilot.IntegrationTests.Devices;

public class OpcUaDeviceConnectorTests
{
    [Fact]
    public async Task Connector_SubscribesToWhitelistedSimulatorStateNode()
    {
        int port = GetFreePort();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpcUa:Port"] = port.ToString()
        }).Build();
        OpcUaEquipmentServer server = new(configuration, NullLogger<OpcUaEquipmentServer>.Instance);
        await server.StartAsync(default);
        await Task.Delay(1500);
        await using OpcUaDeviceConnector connector = new(
            new DeviceConnectionSettings(
                "127.0.0.1",
                port,
                "ns=2;s=Equipment/State",
                PollIntervalMilliseconds: 100,
                AllowInsecure: true),
            8);
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        try
        {
            await connector.ConnectAsync(timeout.Token);
            await using IAsyncEnumerator<DeviceReading> readings = connector.SubscribeAsync(timeout.Token).GetAsyncEnumerator(timeout.Token);
            Assert.True(await readings.MoveNextAsync());
            Assert.Equal("Running", readings.Current.State);
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
