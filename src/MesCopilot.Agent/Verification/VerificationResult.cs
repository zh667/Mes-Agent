namespace MesCopilot.Agent.Verification;

public class VerificationResult
{
    public VerificationStatus Status { get; set; } = VerificationStatus.Unverified;

    public bool IsVerified
    {
        get => Status == VerificationStatus.Verified;
        set => Status = value ? VerificationStatus.Verified : VerificationStatus.Disputed;
    }

    public string Summary { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public DateTime VerifiedAtUtc { get; set; } = DateTime.UtcNow;

    public List<VerificationCheck> Checks { get; set; } = [];

    public List<Discrepancy> Discrepancies { get; set; } = [];

    public int ClaimsChecked { get; set; }

    public int ClaimsVerified { get; set; }
}

public enum VerificationStatus
{
    Verified,
    Disputed,
    Unverified
}

public sealed class VerificationCheck
{
    public string Claim { get; set; } = string.Empty;

    public string ClaimedValue { get; set; } = string.Empty;

    public string ActualValue { get; set; } = string.Empty;

    public string Rule { get; set; } = string.Empty;

    public VerificationStatus Status { get; set; }
}

public class Discrepancy
{
    public string Claim { get; set; } = string.Empty;

    public string ExpectedValue { get; set; } = string.Empty;

    public string ClaimedValue { get; set; } = string.Empty;

    public string Field { get; set; } = string.Empty;
}
