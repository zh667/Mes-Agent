using System.Text.Json;
using MesCopilot.Agent.Models;
using MesCopilot.Application.Services.Verification;

namespace MesCopilot.Agent.Verification.Rules;

public sealed class KnowledgeCitationVerificationRule : IVerificationRule
{
    private const int MaximumCitationCount = 100;
    private readonly IVerificationQueryService _queries;

    public KnowledgeCitationVerificationRule(IVerificationQueryService queries)
    {
        _queries = queries;
    }

    public bool Supports(string toolName) => toolName is "SearchDocuments" or "GetSopByCode";

    public async Task<VerificationResult> VerifyAsync(
        FunctionCallResult result,
        VerificationContext context,
        CancellationToken cancellationToken)
    {
        JsonElement root = VerificationRuleJson.Serialize(result.Data);
        HashSet<int> claimedIds = [];
        CollectDocumentIds(root, claimedIds);
        if (claimedIds.Count == 0)
        {
            return VerificationRuleJson.MissingClaim("KNOWLEDGE_CITATION_MISSING");
        }

        if (claimedIds.Count > MaximumCitationCount)
        {
            return new VerificationResult
            {
                Status = VerificationStatus.Unverified,
                Summary = $"Citation verification is limited to {MaximumCitationCount} documents.",
                ErrorCode = "KNOWLEDGE_CITATION_LIMIT_EXCEEDED"
            };
        }

        IReadOnlySet<int> existing = await _queries.GetExistingDocumentIdsAsync(claimedIds, cancellationToken);
        VerificationResult verification = VerificationRuleJson.Compare(
            "Knowledge citation count",
            claimedIds.Count,
            existing.Count,
            nameof(KnowledgeCitationVerificationRule));
        return verification;
    }

    private static void CollectDocumentIds(JsonElement element, ISet<int> ids)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (property.NameEquals("documentId") && property.Value.TryGetInt32(out int id))
                {
                    ids.Add(id);
                }
                CollectDocumentIds(property.Value, ids);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                CollectDocumentIds(item, ids);
            }
        }
    }
}
