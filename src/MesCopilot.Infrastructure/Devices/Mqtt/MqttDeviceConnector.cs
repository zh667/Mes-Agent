using System.Runtime.CompilerServices;
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using MesCopilot.Domain.Enums;
using MQTTnet;

namespace MesCopilot.Infrastructure.Devices.Mqtt;

public sealed class MqttDeviceConnector : IDeviceConnector
{
    private readonly DeviceConnectionSettings _settings;
    private readonly int _equipmentId;
    private readonly IMqttClient _client;
    private readonly Channel<DeviceReading> _readings = Channel.CreateBounded<DeviceReading>(
        new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = false, SingleWriter = false });

    public MqttDeviceConnector(DeviceConnectionSettings settings, int equipmentId)
    {
        _settings = settings;
        _equipmentId = equipmentId;
        _client = new MqttClientFactory().CreateMqttClient();
        _client.ApplicationMessageReceivedAsync += HandleMessageAsync;
    }

    public DeviceProtocol Protocol => DeviceProtocol.Mqtt;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        MqttClientOptionsBuilder builder = new MqttClientOptionsBuilder()
            .WithClientId($"mes-copilot-{_equipmentId}-{Guid.NewGuid():N}")
            .WithTcpServer(_settings.Host, _settings.Port)
            .WithCleanStart()
            .WithTimeout(TimeSpan.FromSeconds(10));
        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            builder.WithCredentials(_settings.Username, _settings.Password);
        }
        if (_settings.UseTls)
        {
            builder.WithTlsOptions(options => options.UseTls());
        }
        else if (!string.IsNullOrWhiteSpace(_settings.Username) || !string.IsNullOrWhiteSpace(_settings.Password))
        {
            throw new InvalidOperationException("MQTT credentials cannot be sent without TLS.");
        }

        await _client.ConnectAsync(builder.Build(), cancellationToken);
        MqttClientSubscribeOptions subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(filter => filter.WithTopic(_settings.Endpoint))
            .Build();
        await _client.SubscribeAsync(subscribeOptions, cancellationToken);
    }

    public async IAsyncEnumerable<DeviceReading> SubscribeAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (DeviceReading reading in _readings.Reader.ReadAllAsync(cancellationToken))
        {
            yield return reading;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (_client.IsConnected)
        {
            await _client.DisconnectAsync(new MqttClientDisconnectOptions(), cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _client.ApplicationMessageReceivedAsync -= HandleMessageAsync;
        if (_client.IsConnected)
        {
            await _client.DisconnectAsync();
        }
        _client.Dispose();
        _readings.Writer.TryComplete();
    }

    private Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            string json = Encoding.UTF8.GetString(args.ApplicationMessage.Payload.ToArray());
            MqttReadingPayload? payload = JsonSerializer.Deserialize<MqttReadingPayload>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (payload is not null)
            {
                _readings.Writer.TryWrite(new DeviceReading(
                    payload.EquipmentId == 0 ? _equipmentId : payload.EquipmentId,
                    payload.State,
                    payload.Timestamp == default ? DateTime.UtcNow : payload.Timestamp.ToUniversalTime(),
                    payload.Metrics));
            }
        }
        catch (JsonException)
        {
            // Invalid device payloads are ignored; the collector continues receiving later readings.
        }

        return Task.CompletedTask;
    }

    private sealed record MqttReadingPayload(
        int EquipmentId,
        string State,
        DateTime Timestamp,
        IReadOnlyDictionary<string, decimal>? Metrics);
}
