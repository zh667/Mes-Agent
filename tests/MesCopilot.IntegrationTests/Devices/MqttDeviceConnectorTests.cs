using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Configurations;
using MesCopilot.Infrastructure.Devices;
using MesCopilot.Infrastructure.Devices.Mqtt;
using MQTTnet;

namespace MesCopilot.IntegrationTests.Devices;

public sealed class MqttDeviceConnectorTests : IAsyncLifetime
{
    private readonly IContainer _broker = new ContainerBuilder("eclipse-mosquitto:2")
        .WithBindMount(GetMosquittoConfigPath(), "/mosquitto/config/mosquitto.conf", AccessMode.ReadOnly)
        .WithCommand("mosquitto", "-c", "/mosquitto/config/mosquitto.conf")
        .WithPortBinding(1883, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(1883))
        .Build();

    public Task InitializeAsync() => _broker.StartAsync();
    public Task DisposeAsync() => _broker.DisposeAsync().AsTask();

    private static string GetMosquittoConfigPath() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "Devices", "mosquitto-test.conf"));

    [Fact]
    public async Task Connector_ReceivesSimulatorJsonAndMapsEquipmentState()
    {
        int port = _broker.GetMappedPublicPort(1883);
        await using MqttDeviceConnector connector = new(
            new DeviceConnectionSettings("127.0.0.1", port, "mes/equipment/12/state"),
            12);
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        await connector.ConnectAsync(timeout.Token);
        using IMqttClient publisher = new MqttClientFactory().CreateMqttClient();
        await publisher.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", port).Build(), timeout.Token);
        MqttApplicationMessage message = new MqttApplicationMessageBuilder()
            .WithTopic("mes/equipment/12/state")
            .WithPayload(JsonSerializer.Serialize(new { equipmentId = 12, state = "Running", timestamp = DateTime.UtcNow }))
            .Build();
        await publisher.PublishAsync(message, timeout.Token);

        await using IAsyncEnumerator<DeviceReading> readings = connector.SubscribeAsync(timeout.Token).GetAsyncEnumerator(timeout.Token);
        Assert.True(await readings.MoveNextAsync());
        Assert.Equal(12, readings.Current.EquipmentId);
        Assert.Equal("Running", readings.Current.State);
    }
}
