using MesCopilot.Agent.Models;
using MesCopilot.Application.Services.Verification;

namespace MesCopilot.Agent.Verification.Rules;

public sealed class DelayedOrdersVerificationRule : IVerificationRule
{
    private readonly IVerificationQueryService _queries;

    public DelayedOrdersVerificationRule(IVerificationQueryService queries)
    {
        _queries = queries;
    }

    public bool Supports(string toolName) => toolName == "AnalyzeDelayedOrders";

    public async Task<VerificationResult> VerifyAsync(
        FunctionCallResult result,
        VerificationContext context,
        CancellationToken cancellationToken)
    {
        var root = VerificationRuleJson.Serialize(result.Data);
        if (!VerificationRuleJson.TryGetInt32(root, "totalCount", out int claimed))
        {
            return VerificationRuleJson.MissingClaim("DELAYED_ORDER_COUNT_MISSING");
        }

        int actual = await _queries.GetDelayedOrderCountAsync(context.FromUtc, context.ToUtc, cancellationToken);
        return VerificationRuleJson.Compare("Delayed work order count", claimed, actual, nameof(DelayedOrdersVerificationRule));
    }
}
