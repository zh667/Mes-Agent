using System.Net;
using System.Net.Sockets;
using MesCopilot.Domain.Enums;
using MesCopilot.DeviceSimulator.Workers;
using MesCopilot.Infrastructure.Devices;
using MesCopilot.Infrastructure.Devices.Modbus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesCopilot.IntegrationTests.Devices;

public class ModbusTcpDeviceConnectorTests
{
    [Fact]
    public async Task Connector_ReadTimeoutIsIndependentFromPollingInterval()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task serverTask = ServeDelayedReadingAsync(listener, TimeSpan.FromMilliseconds(300), timeout.Token);
        await using ModbusTcpDeviceConnector connector = new(
            new DeviceConnectionSettings(
                "127.0.0.1",
                port,
                "holding",
                UnitId: 1,
                RegisterAddress: 0,
                RegisterCount: 3,
                PollIntervalMilliseconds: 100),
            7);

        try
        {
            await connector.ConnectAsync(timeout.Token);
            await using IAsyncEnumerator<DeviceReading> readings = connector
                .SubscribeAsync(timeout.Token)
                .GetAsyncEnumerator(timeout.Token);

            Assert.True(await readings.MoveNextAsync());
            Assert.Equal(EquipmentState.Running.ToString(), readings.Current.State);
            Assert.Equal(125m, readings.Current.Metrics!["register1"]);
        }
        finally
        {
            await connector.DisconnectAsync(CancellationToken.None);
            listener.Stop();
            await serverTask;
        }
    }

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
        using CancellationTokenSource startupTimeout = new(TimeSpan.FromSeconds(10));
        await server.WaitUntilStartedAsync(startupTimeout.Token);
        await using ModbusTcpDeviceConnector connector = new(
            new DeviceConnectionSettings("127.0.0.1", port, "holding", UnitId: 1, RegisterAddress: 0, RegisterCount: 3, PollIntervalMilliseconds: 100),
            7);
        try
        {
            using CancellationTokenSource connectTimeout = new(TimeSpan.FromSeconds(10));
            await connector.ConnectAsync(connectTimeout.Token);
            using CancellationTokenSource firstReadingTimeout = new(TimeSpan.FromSeconds(10));
            await using IAsyncEnumerator<DeviceReading> readings = connector
                .SubscribeAsync(firstReadingTimeout.Token)
                .GetAsyncEnumerator(firstReadingTimeout.Token);
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

    private static async Task ServeDelayedReadingAsync(
        TcpListener listener,
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        using TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken);
        await using NetworkStream stream = client.GetStream();
        byte[] request = new byte[12];
        await stream.ReadExactlyAsync(request, cancellationToken);
        await Task.Delay(delay, cancellationToken);

        byte[] response =
        [
            request[0], request[1],
            0, 0,
            0, 9,
            request[6],
            3, 6,
            0, (byte)EquipmentState.Running,
            0, 125,
            0, 98
        ];
        await stream.WriteAsync(response, cancellationToken);
    }
}
