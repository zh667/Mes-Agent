using System.Net;
using System.Net.Sockets;
using MesCopilot.Domain.Enums;
using NModbus;
using NModbus.Data;

namespace MesCopilot.DeviceSimulator.Workers;

public sealed class ModbusEquipmentServer : BackgroundService
{
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly IConfiguration _configuration;
    private readonly ILogger<ModbusEquipmentServer> _logger;

    public ModbusEquipmentServer(IConfiguration configuration, ILogger<ModbusEquipmentServer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task WaitUntilStartedAsync(CancellationToken cancellationToken = default) =>
        _started.Task.WaitAsync(cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int port = _configuration.GetValue("Modbus:Port", 5020);
        byte unitId = _configuration.GetValue<byte>("Modbus:UnitId", 1);
        TcpListener listener = new(IPAddress.Any, port);
        listener.Start();
        ModbusFactory factory = new();
        ISlaveDataStore dataStore = new DefaultSlaveDataStore();
        IModbusSlave slave = factory.CreateSlave(unitId, dataStore);
        IModbusSlaveNetwork network = factory.CreateSlaveNetwork(listener);
        network.AddSlave(slave);
        dataStore.HoldingRegisters.WritePoints(0, [(ushort)EquipmentState.Running, 125, 98]);
        Task listenTask = network.ListenAsync(stoppingToken);
        _started.TrySetResult();
        _logger.LogInformation("Modbus TCP simulator listening on port {Port} with unit id {UnitId}.", port, unitId);
        try
        {
            await listenTask;
        }
        finally
        {
            listener.Stop();
        }
    }
}
