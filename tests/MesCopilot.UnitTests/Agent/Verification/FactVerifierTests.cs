using MesCopilot.Agent.Models;
using MesCopilot.Agent.Verification;

namespace MesCopilot.UnitTests.Agent.Verification;

public class FactVerifierTests
{
    private readonly FactVerifier _verifier = new();

    [Fact]
    public void Verify_WhenDataMatchesExplanation_ReturnsVerified()
    {
        FunctionCallResult result = new()
        {
            Data = new { totalCount = 3, workOrders = new[] { "WO-001", "WO-002", "WO-003" } },
            Explanation = "There are 3 work orders currently in production."
        };

        VerificationResult verification = _verifier.Verify(result);

        Assert.True(verification.IsVerified);
        Assert.Empty(verification.Discrepancies);
        Assert.Equal(1, verification.ClaimsChecked);
        Assert.Equal(1, verification.ClaimsVerified);
    }

    [Fact]
    public void Verify_WhenCountMismatch_ReturnsDiscrepancy()
    {
        FunctionCallResult result = new()
        {
            Data = new { totalCount = 5, workOrders = new[] { "WO-001", "WO-002", "WO-003", "WO-004", "WO-005" } },
            Explanation = "There are 3 work orders currently in production."
        };

        VerificationResult verification = _verifier.Verify(result);

        Assert.False(verification.IsVerified);
        Discrepancy discrepancy = Assert.Single(verification.Discrepancies);
        Assert.Equal("3", discrepancy.ClaimedValue);
        Assert.Equal("5", discrepancy.ExpectedValue);
    }

    [Fact]
    public void Verify_WhenPercentageMatchesData_ReturnsVerified()
    {
        FunctionCallResult result = new()
        {
            Data = new { oee = 82.5, availability = 92.0, performance = 95.0, quality = 94.5 },
            Explanation = "The equipment OEE is 82.5%, and availability is 92.0%."
        };

        VerificationResult verification = _verifier.Verify(result);

        Assert.True(verification.IsVerified);
        Assert.Equal(2, verification.ClaimsChecked);
    }

    [Fact]
    public void Verify_WhenNullData_ReturnsVerified()
    {
        FunctionCallResult result = new()
        {
            Data = null,
            Explanation = "No related data was found."
        };

        VerificationResult verification = _verifier.Verify(result);

        Assert.True(verification.IsVerified);
        Assert.Equal(0, verification.ClaimsChecked);
    }

    [Fact]
    public void Verify_WhenEmptyExplanation_ReturnsVerified()
    {
        FunctionCallResult result = new()
        {
            Data = new { totalCount = 5 },
            Explanation = ""
        };

        VerificationResult verification = _verifier.Verify(result);

        Assert.True(verification.IsVerified);
        Assert.Equal(0, verification.ClaimsChecked);
    }

    [Fact]
    public void Verify_TracksMultipleClaimCounts()
    {
        FunctionCallResult result = new()
        {
            Data = new { totalCount = 2, delayHours = 8 },
            Explanation = "There are 2 delayed work orders, and the longest delay is 8 hours."
        };

        VerificationResult verification = _verifier.Verify(result);

        Assert.True(verification.IsVerified);
        Assert.Equal(2, verification.ClaimsChecked);
        Assert.Equal(2, verification.ClaimsVerified);
    }

    [Fact]
    public void Verify_WhenEnglishUnitsAndTrailingZerosMatch_ReturnsVerified()
    {
        FunctionCallResult result = new()
        {
            Data = new { totalCount = 3, oee = 82.5 },
            Explanation = "There are 3 work orders with OEE at 82.500%."
        };

        VerificationResult verification = _verifier.Verify(result);

        Assert.True(verification.IsVerified);
        Assert.Equal(2, verification.ClaimsChecked);
        Assert.Equal(2, verification.ClaimsVerified);
    }

    [Fact]
    public void Verify_WhenScientificNotationMatches_ReturnsVerified()
    {
        FunctionCallResult result = new()
        {
            Data = new { delayHours = 1000 },
            Explanation = "The longest delay is 1e3 hours."
        };

        VerificationResult verification = _verifier.Verify(result);

        Assert.True(verification.IsVerified);
        Assert.Equal(1, verification.ClaimsChecked);
        Assert.Equal(1, verification.ClaimsVerified);
    }
}
