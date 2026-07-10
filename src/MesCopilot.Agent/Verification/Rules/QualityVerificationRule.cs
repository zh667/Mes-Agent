using MesCopilot.Agent.Models;
using MesCopilot.Application.Services.Verification;

namespace MesCopilot.Agent.Verification.Rules;

public sealed class QualityVerificationRule : IVerificationRule
{
    private readonly IVerificationQueryService _queries;

    public QualityVerificationRule(IVerificationQueryService queries)
    {
        _queries = queries;
    }

    public bool Supports(string toolName) => toolName == "TraceBatch";

    public async Task<VerificationResult> VerifyAsync(
        FunctionCallResult result,
        VerificationContext context,
        CancellationToken cancellationToken)
    {
        var root = VerificationRuleJson.Serialize(result.Data);
        if (!VerificationRuleJson.TryGetString(root, "batchNumber", out string batchNumber) ||
            !VerificationRuleJson.TryGetInt32(root, "productionReportCount", out int reportCount) ||
            !VerificationRuleJson.TryGetInt32(root, "inspectionCount", out int inspectionCount))
        {
            return VerificationRuleJson.MissingClaim("QUALITY_TRACE_CLAIM_MISSING");
        }

        VerifiedBatchTrace? actual = await _queries.GetBatchTraceAsync(batchNumber, cancellationToken);
        if (actual is null)
        {
            return VerificationRuleJson.MissingClaim("QUALITY_TRACE_NOT_FOUND");
        }

        VerificationResult reports = VerificationRuleJson.Compare(
            "Production report count",
            reportCount,
            actual.ProductionReportCount,
            nameof(QualityVerificationRule));
        VerificationResult inspections = VerificationRuleJson.Compare(
            "Inspection count",
            inspectionCount,
            actual.InspectionCount,
            nameof(QualityVerificationRule));
        reports.Checks.AddRange(inspections.Checks);
        reports.Discrepancies.AddRange(inspections.Discrepancies);
        reports.ClaimsChecked = 2;
        reports.ClaimsVerified += inspections.ClaimsVerified;
        reports.Status = reports.Checks.All(check => check.Status == VerificationStatus.Verified)
            ? VerificationStatus.Verified
            : VerificationStatus.Disputed;
        reports.Summary = reports.Status == VerificationStatus.Verified
            ? "The batch trace counts match the primary MES data."
            : "One or more batch trace counts differ from the primary MES data.";
        return reports;
    }
}
