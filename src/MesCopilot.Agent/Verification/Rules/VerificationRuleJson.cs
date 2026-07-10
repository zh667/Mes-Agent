using System.Globalization;
using System.Text.Json;

namespace MesCopilot.Agent.Verification.Rules;

internal static class VerificationRuleJson
{
    public static JsonElement Serialize(object? data)
    {
        return JsonSerializer.SerializeToElement(data, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public static bool TryGetInt32(JsonElement root, string name, out int value)
    {
        value = default;
        return root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty(name, out JsonElement property) &&
            property.TryGetInt32(out value);
    }

    public static bool TryGetDecimal(JsonElement root, string name, out decimal value)
    {
        value = default;
        return root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty(name, out JsonElement property) &&
            property.TryGetDecimal(out value);
    }

    public static bool TryGetString(JsonElement root, string name, out string value)
    {
        value = string.Empty;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty(name, out JsonElement property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    public static VerificationResult Compare(
        string claim,
        decimal claimed,
        decimal actual,
        string rule,
        decimal tolerance = 0m)
    {
        bool matches = Math.Abs(claimed - actual) <= tolerance;
        VerificationStatus status = matches ? VerificationStatus.Verified : VerificationStatus.Disputed;
        return new VerificationResult
        {
            Status = status,
            Summary = matches ? "The claim matches the primary MES data." : "The claim differs from the primary MES data.",
            ClaimsChecked = 1,
            ClaimsVerified = matches ? 1 : 0,
            Checks =
            [
                new VerificationCheck
                {
                    Claim = claim,
                    ClaimedValue = claimed.ToString("0.################", CultureInfo.InvariantCulture),
                    ActualValue = actual.ToString("0.################", CultureInfo.InvariantCulture),
                    Rule = rule,
                    Status = status
                }
            ],
            Discrepancies = matches
                ? []
                :
                [
                    new Discrepancy
                    {
                        Claim = claim,
                        ClaimedValue = claimed.ToString("0.################", CultureInfo.InvariantCulture),
                        ExpectedValue = actual.ToString("0.################", CultureInfo.InvariantCulture),
                        Field = rule
                    }
                ]
        };
    }

    public static VerificationResult MissingClaim(string errorCode)
    {
        return new VerificationResult
        {
            Status = VerificationStatus.Unverified,
            Summary = "The tool result did not contain the fields required for verification.",
            ErrorCode = errorCode
        };
    }
}
