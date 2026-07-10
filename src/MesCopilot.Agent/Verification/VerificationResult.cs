namespace MesCopilot.Agent.Verification;

public class VerificationResult
{
    public bool IsVerified { get; set; }

    public List<Discrepancy> Discrepancies { get; set; } = [];

    public int ClaimsChecked { get; set; }

    public int ClaimsVerified { get; set; }
}

public class Discrepancy
{
    public string Claim { get; set; } = string.Empty;

    public string ExpectedValue { get; set; } = string.Empty;

    public string ClaimedValue { get; set; } = string.Empty;

    public string Field { get; set; } = string.Empty;
}
