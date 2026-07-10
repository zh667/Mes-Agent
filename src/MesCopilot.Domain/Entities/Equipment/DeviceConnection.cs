using MesCopilot.Domain.Common;
using MesCopilot.Domain.Enums;

namespace MesCopilot.Domain.Entities.Equipment;

public sealed class DeviceConnection : ITenantEntity
{
    public int Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int EquipmentId { get; set; }
    public DeviceProtocol Protocol { get; set; }
    public string EncryptedConfiguration { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public bool HasCredentials { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? LastConnectedAt { get; set; }
    public string? LastErrorCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Equipment Equipment { get; set; } = null!;
}
