using System.Net.Sockets;
using System.Runtime.CompilerServices;
using MesCopilot.Domain.Enums;
using NModbus;

namespace MesCopilot.Infrastructure.Devices.Modbus;

public sealed class ModbusTcpDeviceConnector : IDeviceConnector
{
    private readonly DeviceConnectionSettings _settings;
    private readonly int _equipmentId;
    private TcpClient? _tcpClient;
    private IModbusMaster? _master;

    public ModbusTcpDeviceConnector(DeviceConnectionSettings settings, int equipmentId)
    {
        _settings = settings;
        _equipmentId = equipmentId;
    }

    public DeviceProtocol Protocol => DeviceProtocol.ModbusTcp;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if ((int)_settings.RegisterAddress + _settings.RegisterCount > ushort.MaxValue + 1)
        {
            throw new ArgumentOutOfRangeException(nameof(_settings.RegisterCount), "The Modbus register range exceeds 65535.");
        }

        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(_settings.Host, _settings.Port, cancellationToken);
        _tcpClient.ReceiveTimeout = Math.Max(100, _settings.PollIntervalMilliseconds);
        _tcpClient.SendTimeout = Math.Max(100, _settings.PollIntervalMilliseconds);
        _master = new ModbusFactory().CreateMaster(_tcpClient);
        _master.Transport.Retries = 0;
        _master.Transport.ReadTimeout = Math.Max(100, _settings.PollIntervalMilliseconds);
    }

    public async IAsyncEnumerable<DeviceReading> SubscribeAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IModbusMaster master = _master ?? throw new InvalidOperationException("Modbus connector is not connected.");
        while (!cancellationToken.IsCancellationRequested)
        {
            ushort[] registers = await master.ReadHoldingRegistersAsync(
                _settings.UnitId,
                _settings.RegisterAddress,
                _settings.RegisterCount);
            EquipmentState state = registers.Length == 0
                ? EquipmentState.Offline
                : Enum.IsDefined(typeof(EquipmentState), (int)registers[0])
                    ? (EquipmentState)registers[0]
                    : EquipmentState.Alarm;
            Dictionary<string, decimal> metrics = registers
                .Select((value, index) => new { value, index })
                .ToDictionary(item => $"register{_settings.RegisterAddress + item.index}", item => (decimal)item.value);
            yield return new DeviceReading(_equipmentId, state.ToString(), DateTime.UtcNow, metrics);
            await Task.Delay(_settings.PollIntervalMilliseconds, cancellationToken);
        }
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _master?.Dispose();
        _master = null;
        _tcpClient?.Dispose();
        _tcpClient = null;
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync(CancellationToken.None);
    }
}
