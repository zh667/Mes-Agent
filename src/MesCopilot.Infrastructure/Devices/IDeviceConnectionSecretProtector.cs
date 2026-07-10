namespace MesCopilot.Infrastructure.Devices;

public interface IDeviceConnectionSecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
}
