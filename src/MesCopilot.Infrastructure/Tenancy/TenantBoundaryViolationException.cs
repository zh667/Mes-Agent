namespace MesCopilot.Infrastructure.Tenancy;

public sealed class TenantBoundaryViolationException : InvalidOperationException
{
    public TenantBoundaryViolationException(string message)
        : base(message)
    {
    }
}
