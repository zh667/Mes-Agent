using MesCopilot.Domain.Enums;

namespace MesCopilot.Application.Dtos.Devices;

public sealed record DeviceConnectionDto(
    int Id,
    string Name,
    int EquipmentId,
    string EquipmentCode,
    DeviceProtocol Protocol,
    string Host,
    int Port,
    string Endpoint,
    bool HasCredentials,
    bool UseTls,
    bool AllowInsecure,
    bool IsEnabled,
    DateTime? LastConnectedAt,
    string? LastErrorCode);

public sealed record DeviceConnectionInput(
    string Name,
    int EquipmentId,
    DeviceProtocol Protocol,
    string Host,
    int Port,
    string Endpoint,
    string? Username,
    string? Password,
    string? CertificateThumbprint,
    byte UnitId,
    ushort RegisterAddress,
    ushort RegisterCount,
    int PollIntervalMilliseconds,
    bool UseTls,
    bool AllowInsecure);

public sealed record DeviceConnectionRuntime(
    int Id,
    string TenantId,
    int EquipmentId,
    DeviceProtocol Protocol,
    MesCopilot.Infrastructure.Devices.DeviceConnectionSettings Settings);
