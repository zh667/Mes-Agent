using System.Text.Json;
using MesCopilot.Application.Dtos.Devices;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Devices;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Application.Services.Devices;

public sealed class DeviceConnectionService : IDeviceConnectionService
{
    private readonly MesDbContext _context;
    private readonly IDeviceConnectionSecretProtector _protector;
    private readonly IDeviceConnectorFactory _connectorFactory;

    public DeviceConnectionService(
        MesDbContext context,
        IDeviceConnectionSecretProtector protector,
        IDeviceConnectorFactory connectorFactory)
    {
        _context = context;
        _protector = protector;
        _connectorFactory = connectorFactory;
    }

    public async Task<IReadOnlyList<DeviceConnectionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<DeviceConnection> connections = await _context.DeviceConnections.AsNoTracking()
            .Include(connection => connection.Equipment)
            .OrderBy(connection => connection.Equipment.Code)
            .ThenBy(connection => connection.Name)
            .ToListAsync(cancellationToken);
        return connections.Select(connection => ToDto(connection, Unprotect(connection))).ToList();
    }

    public async Task<DeviceConnectionDto> CreateAsync(DeviceConnectionInput input, CancellationToken cancellationToken = default)
    {
        Validate(input);
        DeviceConnectionSettings settings = ToSettings(input);
        ValidateProtocolSettings(input.Protocol, settings);
        DeviceConnection connection = new()
        {
            Name = input.Name.Trim(),
            EquipmentId = input.EquipmentId,
            Protocol = input.Protocol,
            EncryptedConfiguration = _protector.Protect(JsonSerializer.Serialize(settings)),
            Host = settings.Host,
            Port = settings.Port,
            Endpoint = settings.Endpoint,
            HasCredentials = !string.IsNullOrWhiteSpace(settings.Username) || !string.IsNullOrWhiteSpace(settings.Password)
        };
        _context.DeviceConnections.Add(connection);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetDtoAsync(connection.Id, cancellationToken);
    }

    public async Task<DeviceConnectionDto?> UpdateAsync(int id, DeviceConnectionInput input, CancellationToken cancellationToken = default)
    {
        Validate(input);
        DeviceConnection? connection = await _context.DeviceConnections.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (connection is null) return null;
        DeviceConnectionSettings old = Unprotect(connection);
        DeviceConnectionSettings settings = ToSettings(input) with
        {
            Username = KeepExistingWhenBlank(input.Username, old.Username),
            Password = KeepExistingWhenBlank(input.Password, old.Password),
            CertificateThumbprint = KeepExistingWhenBlank(input.CertificateThumbprint, old.CertificateThumbprint)
        };
        ValidateProtocolSettings(input.Protocol, settings);
        connection.Name = input.Name.Trim();
        connection.EquipmentId = input.EquipmentId;
        connection.Protocol = input.Protocol;
        connection.EncryptedConfiguration = _protector.Protect(JsonSerializer.Serialize(settings));
        connection.Host = settings.Host;
        connection.Port = settings.Port;
        connection.Endpoint = settings.Endpoint;
        connection.HasCredentials = !string.IsNullOrWhiteSpace(settings.Username) || !string.IsNullOrWhiteSpace(settings.Password);
        connection.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return await GetDtoAsync(connection.Id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        DeviceConnection? connection = await _context.DeviceConnections.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (connection is null) return false;
        _context.DeviceConnections.Remove(connection);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetEnabledAsync(int id, bool enabled, CancellationToken cancellationToken = default)
    {
        DeviceConnection? connection = await _context.DeviceConnections.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (connection is null) return false;
        connection.IsEnabled = enabled;
        connection.UpdatedAt = DateTime.UtcNow;
        if (!enabled) connection.LastErrorCode = null;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TestAsync(int id, CancellationToken cancellationToken = default)
    {
        DeviceConnectionRuntime? runtime = await GetRuntimeAsync(id, cancellationToken);
        if (runtime is null) return false;
        await using IDeviceConnector connector = _connectorFactory.Create(runtime.Protocol, runtime.Settings, runtime.EquipmentId);
        await connector.ConnectAsync(cancellationToken);
        await connector.DisconnectAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<DeviceConnectionRuntime>> GetEnabledRuntimeAsync(CancellationToken cancellationToken = default)
    {
        List<DeviceConnection> connections = await _context.DeviceConnections.AsNoTracking()
            .Where(connection => connection.IsEnabled)
            .ToListAsync(cancellationToken);
        return connections.Select(ToRuntime).ToList();
    }

    public async Task<DeviceConnectionRuntime?> GetRuntimeAsync(int id, CancellationToken cancellationToken = default)
    {
        DeviceConnection? connection = await _context.DeviceConnections.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return connection is null ? null : ToRuntime(connection);
    }

    public async Task RecordConnectionResultAsync(int id, bool connected, string? errorCode, CancellationToken cancellationToken = default)
    {
        DeviceConnection? connection = await _context.DeviceConnections.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (connection is null) return;
        connection.LastConnectedAt = connected ? DateTime.UtcNow : connection.LastConnectedAt;
        connection.LastErrorCode = errorCode;
        connection.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<DeviceConnectionDto> GetDtoAsync(int id, CancellationToken cancellationToken)
    {
        DeviceConnection connection = await _context.DeviceConnections.AsNoTracking()
            .Include(item => item.Equipment)
            .Where(connection => connection.Id == id)
            .SingleAsync(cancellationToken);
        return ToDto(connection, Unprotect(connection));
    }

    private DeviceConnectionRuntime ToRuntime(DeviceConnection connection) =>
        new(connection.Id, connection.TenantId, connection.EquipmentId, connection.Protocol, Unprotect(connection));

    private DeviceConnectionSettings Unprotect(DeviceConnection connection) =>
        JsonSerializer.Deserialize<DeviceConnectionSettings>(_protector.Unprotect(connection.EncryptedConfiguration))
        ?? throw new InvalidOperationException("Device connection configuration is invalid.");

    private static DeviceConnectionDto ToDto(DeviceConnection connection, DeviceConnectionSettings settings) => new(
        connection.Id, connection.Name, connection.EquipmentId, connection.Equipment.Code, connection.Protocol,
        connection.Host, connection.Port, connection.Endpoint, connection.HasCredentials, settings.UseTls,
        settings.AllowInsecure, connection.IsEnabled,
        connection.LastConnectedAt, connection.LastErrorCode);

    private static DeviceConnectionSettings ToSettings(DeviceConnectionInput input) => new(
        input.Host.Trim(), input.Port, input.Endpoint.Trim(), input.Username, input.Password,
        input.CertificateThumbprint, input.UnitId, input.RegisterAddress, input.RegisterCount,
        input.PollIntervalMilliseconds, input.UseTls, input.AllowInsecure);

    private static string? KeepExistingWhenBlank(string? replacement, string? existing) =>
        string.IsNullOrWhiteSpace(replacement) ? existing : replacement;

    private static void ValidateProtocolSettings(DeviceProtocol protocol, DeviceConnectionSettings settings)
    {
        if (protocol == DeviceProtocol.Mqtt &&
            (!string.IsNullOrWhiteSpace(settings.Username) || !string.IsNullOrWhiteSpace(settings.Password)) &&
            !settings.UseTls)
        {
            throw new ArgumentException("MQTT credentials require TLS.");
        }

        if (protocol == DeviceProtocol.OpcUa &&
            !settings.AllowInsecure &&
            string.IsNullOrWhiteSpace(settings.CertificateThumbprint))
        {
            throw new ArgumentException("Secure OPC UA requires a trusted certificate thumbprint.");
        }
    }

    private static void Validate(DeviceConnectionInput input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Host);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Endpoint);
        if (input.EquipmentId <= 0) throw new ArgumentOutOfRangeException(nameof(input.EquipmentId));
        if (input.Port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(input.Port));
        if (input.PollIntervalMilliseconds is < 100 or > 60_000) throw new ArgumentOutOfRangeException(nameof(input.PollIntervalMilliseconds));
        if (input.RegisterCount is 0 or > 125) throw new ArgumentOutOfRangeException(nameof(input.RegisterCount));
        if ((int)input.RegisterAddress + input.RegisterCount > ushort.MaxValue + 1)
        {
            throw new ArgumentOutOfRangeException(nameof(input.RegisterCount), "The Modbus register range exceeds 65535.");
        }
    }
}
