using System.ComponentModel.DataAnnotations;
using MesCopilot.Domain.Enums;

namespace MesCopilot.Api.Dtos.Devices;

public sealed class DeviceConnectionRequest : IValidatableObject
{
    [Required, MaxLength(200)] public string Name { get; init; } = string.Empty;
    [Range(1, int.MaxValue)] public int EquipmentId { get; init; }
    public DeviceProtocol Protocol { get; init; }
    [Required, MaxLength(255)] public string Host { get; init; } = string.Empty;
    [Range(1, 65535)] public int Port { get; init; }
    [Required, MaxLength(500)] public string Endpoint { get; init; } = string.Empty;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? CertificateThumbprint { get; init; }
    [Range(1, 247)] public byte UnitId { get; init; } = 1;
    public ushort RegisterAddress { get; init; }
    [Range(1, 125)] public ushort RegisterCount { get; init; } = 1;
    [Range(100, 60_000)] public int PollIntervalMilliseconds { get; init; } = 1000;
    public bool UseTls { get; init; }
    public bool AllowInsecure { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Protocol == DeviceProtocol.Mqtt &&
            (!string.IsNullOrWhiteSpace(Username) || !string.IsNullOrWhiteSpace(Password)) &&
            !UseTls)
        {
            yield return new ValidationResult("MQTT credentials require TLS.", [nameof(UseTls)]);
        }

        if (Protocol == DeviceProtocol.OpcUa && !AllowInsecure && string.IsNullOrWhiteSpace(CertificateThumbprint))
        {
            yield return new ValidationResult(
                "Secure OPC UA requires a trusted certificate thumbprint.",
                [nameof(CertificateThumbprint)]);
        }

        if (Protocol == DeviceProtocol.ModbusTcp && (int)RegisterAddress + RegisterCount > ushort.MaxValue + 1)
        {
            yield return new ValidationResult(
                "The Modbus register range exceeds 65535.",
                [nameof(RegisterAddress), nameof(RegisterCount)]);
        }
    }
}
