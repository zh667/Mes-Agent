using MesCopilot.Agent.Verification;

namespace MesCopilot.Api.Dtos.Agent;

public sealed record VerificationResultDto(
    string Status,
    string Summary,
    string? ErrorCode,
    DateTime VerifiedAtUtc,
    IReadOnlyList<VerificationCheckDto> Checks)
{
    public static VerificationResultDto FromDomain(VerificationResult result)
    {
        return new VerificationResultDto(
            result.Status.ToString(),
            result.Summary,
            result.ErrorCode,
            result.VerifiedAtUtc,
            result.Checks.Select(VerificationCheckDto.FromDomain).ToList());
    }
}

public sealed record VerificationCheckDto(
    string Claim,
    string ClaimedValue,
    string ActualValue,
    string Rule,
    string Status)
{
    internal static VerificationCheckDto FromDomain(VerificationCheck check)
    {
        return new VerificationCheckDto(
            check.Claim,
            check.ClaimedValue,
            check.ActualValue,
            check.Rule,
            check.Status.ToString());
    }
}
