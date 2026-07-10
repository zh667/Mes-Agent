using Microsoft.AspNetCore.DataProtection;

namespace MesCopilot.Infrastructure.Devices;

public sealed class DataProtectionDeviceConnectionSecretProtector : IDeviceConnectionSecretProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionDeviceConnectionSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("MesCopilot.DeviceConnections.v1");
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);
    public string Unprotect(string protectedValue) => _protector.Unprotect(protectedValue);
}
