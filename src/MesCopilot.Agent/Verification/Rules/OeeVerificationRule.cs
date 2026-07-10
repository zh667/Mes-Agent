using System.Text.Json;
using MesCopilot.Agent.Models;
using MesCopilot.Application.Services.Verification;

namespace MesCopilot.Agent.Verification.Rules;

public sealed class OeeVerificationRule : IVerificationRule
{
    private const decimal Tolerance = 0.0001m;
    private readonly IVerificationQueryService _queries;

    public OeeVerificationRule(IVerificationQueryService queries)
    {
        _queries = queries;
    }

    public bool Supports(string toolName) => toolName == "CalculateOee";

    public async Task<VerificationResult> VerifyAsync(
        FunctionCallResult result,
        VerificationContext context,
        CancellationToken cancellationToken)
    {
        JsonElement root = VerificationRuleJson.Serialize(result.Data);
        if (!VerificationRuleJson.TryGetInt32(root, "equipmentId", out int equipmentId) ||
            !VerificationRuleJson.TryGetDecimal(root, "oee", out decimal claimed) ||
            !root.TryGetProperty("date", out JsonElement dateElement) ||
            !dateElement.TryGetDateTime(out DateTime date))
        {
            return VerificationRuleJson.MissingClaim("OEE_CLAIM_MISSING");
        }

        VerifiedOeeSnapshot? actual = await _queries.GetOeeAsync(equipmentId, date, cancellationToken);
        return actual is null
            ? VerificationRuleJson.MissingClaim("OEE_SOURCE_NOT_FOUND")
            : VerificationRuleJson.Compare("OEE", claimed, actual.Oee, nameof(OeeVerificationRule), Tolerance);
    }
}
