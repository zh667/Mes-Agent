using MesCopilot.DeviceSimulator.Services;
using MesCopilot.DeviceSimulator.Workers;
using MesCopilot.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMesCopilotInfrastructure(builder.Configuration, builder.Environment.EnvironmentName);
builder.Services.AddSingleton<EquipmentStateCalculator>();
builder.Services.AddHostedService<EquipmentSimulatorWorker>();
builder.Services.AddHostedService<MqttEquipmentPublisher>();
builder.Services.AddHostedService<ModbusEquipmentServer>();
builder.Services.AddHostedService<OpcUaEquipmentServer>();

var host = builder.Build();
host.Run();
