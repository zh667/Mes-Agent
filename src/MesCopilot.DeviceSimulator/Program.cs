using MesCopilot.DeviceSimulator.Services;
using MesCopilot.DeviceSimulator.Workers;
using MesCopilot.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMesCopilotInfrastructure(builder.Configuration);
builder.Services.AddSingleton<EquipmentStateCalculator>();
builder.Services.AddHostedService<EquipmentSimulatorWorker>();

var host = builder.Build();
host.Run();
