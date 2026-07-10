using System.Text.Json;
using MesCopilot.DeviceSimulator.Services;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using MQTTnet;

namespace MesCopilot.DeviceSimulator.Workers;

public sealed class MqttEquipmentPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EquipmentStateCalculator _calculator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MqttEquipmentPublisher> _logger;

    public MqttEquipmentPublisher(
        IServiceScopeFactory scopeFactory,
        EquipmentStateCalculator calculator,
        IConfiguration configuration,
        ILogger<MqttEquipmentPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _calculator = calculator;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string host = _configuration["Mqtt:Host"] ?? "localhost";
        int port = _configuration.GetValue("Mqtt:Port", 1883);
        using IMqttClient client = new MqttClientFactory().CreateMqttClient();
        MqttClientOptionsBuilder optionsBuilder = new MqttClientOptionsBuilder()
            .WithClientId($"mes-simulator-{Guid.NewGuid():N}")
            .WithTcpServer(host, port)
            .WithCleanStart();
        string? username = _configuration["Mqtt:Username"];
        if (!string.IsNullOrWhiteSpace(username))
        {
            optionsBuilder.WithCredentials(username, _configuration["Mqtt:Password"]);
        }
        MqttClientOptions options = optionsBuilder.Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!client.IsConnected) await client.ConnectAsync(options, stoppingToken);
                await PublishAsync(client, stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(exception, "MQTT simulator publish failed.");
            }
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task PublishAsync(IMqttClient client, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        CurrentTenantContext tenant = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
        tenant.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: true));
        MesDbContext context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
        var equipment = await context.Equipment.AsNoTracking().Where(item => item.IsActive).ToListAsync(cancellationToken);
        foreach (var item in equipment)
        {
            var state = _calculator.DetermineState(item, null);
            string payload = JsonSerializer.Serialize(new
            {
                equipmentId = item.Id,
                state = state.ToString(),
                timestamp = DateTime.UtcNow,
                metrics = new { cycleTime = item.IdealCycleTime }
            });
            MqttApplicationMessage message = new MqttApplicationMessageBuilder()
                .WithTopic($"mes/equipment/{item.Id}/state")
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            await client.PublishAsync(message, cancellationToken);
        }
    }
}
