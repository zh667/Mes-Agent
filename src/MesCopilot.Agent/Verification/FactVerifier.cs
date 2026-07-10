using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using MesCopilot.Agent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesCopilot.Agent.Verification;

public partial class FactVerifier : IFactVerifier
{
    private const decimal NumericTolerance = 0.0001m;
    private readonly IReadOnlyList<IVerificationRule> _rules;
    private readonly ILogger<FactVerifier> _logger;

    public FactVerifier()
        : this([], NullLogger<FactVerifier>.Instance)
    {
    }

    public FactVerifier(IEnumerable<IVerificationRule> rules)
        : this(rules, NullLogger<FactVerifier>.Instance)
    {
    }

    public FactVerifier(IEnumerable<IVerificationRule> rules, ILogger<FactVerifier> logger)
    {
        _rules = rules.ToList();
        _logger = logger;
    }

    public async Task<VerificationResult> VerifyAsync(
        FunctionCallResult result,
        VerificationContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IVerificationRule? rule = _rules.FirstOrDefault(candidate => candidate.Supports(context.ToolName));
        if (rule is null)
        {
            return new VerificationResult
            {
                Status = VerificationStatus.Unverified,
                Summary = "No approved verification rule is available for this tool result.",
                ErrorCode = "VERIFICATION_RULE_NOT_FOUND"
            };
        }

        try
        {
            return await rule.VerifyAsync(result, context, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Verification query failed for tool {ToolName} with correlation {CorrelationId}.",
                context.ToolName,
                context.CorrelationId);
            return new VerificationResult
            {
                Status = VerificationStatus.Unverified,
                Summary = "The verification query could not be completed.",
                ErrorCode = "VERIFICATION_QUERY_FAILED"
            };
        }
    }

    public VerificationResult Verify(FunctionCallResult result)
    {
        VerificationResult verification = new()
        {
            IsVerified = true
        };

        if (result.Data is null || string.IsNullOrWhiteSpace(result.Explanation))
        {
            return verification;
        }

        string dataJson = JsonSerializer.Serialize(result.Data);
        using JsonDocument document = JsonDocument.Parse(dataJson);
        JsonElement root = document.RootElement;

        foreach (NumericalClaim claim in ExtractClaims(result.Explanation))
        {
            verification.ClaimsChecked++;
            decimal? actualValue = FindActualValue(root, claim);
            if (actualValue.HasValue && NumbersMatch(actualValue.Value, claim.Value))
            {
                verification.ClaimsVerified++;
                continue;
            }

            verification.IsVerified = false;
            verification.Discrepancies.Add(new Discrepancy
            {
                Claim = claim.Text,
                ClaimedValue = FormatDecimal(claim.Value),
                ExpectedValue = actualValue.HasValue ? FormatDecimal(actualValue.Value) : "N/A",
                Field = claim.Field
            });
        }

        return verification;
    }

    private static IEnumerable<NumericalClaim> ExtractClaims(string explanation)
    {
        foreach (Match match in NumberWithUnitRegex().Matches(explanation))
        {
            if (!TryParseDecimal(match.Groups["value"].Value, out decimal value))
            {
                continue;
            }

            string suffix = match.Groups["suffix"].Value;
            string context = GetSurroundingText(explanation, match.Index, 24);

            yield return new NumericalClaim(
                Text: match.Value,
                Value: value,
                Field: InferField(context, suffix),
                Type: IsPercentageSuffix(suffix) ? ClaimType.Percentage : ClaimType.Count);
        }
    }

    private static decimal? FindActualValue(JsonElement root, NumericalClaim claim)
    {
        IEnumerable<string> fields = claim.Type == ClaimType.Count
            ? CandidateCountFields(claim.Field)
            : CandidateDecimalFields(claim.Field);

        foreach (string field in fields)
        {
            if (TryFindNumber(root, field, out decimal value))
            {
                return value;
            }
        }

        if (claim.Type == ClaimType.Count)
        {
            foreach (string field in CandidateDecimalFields(claim.Field))
            {
                if (TryFindNumber(root, field, out decimal value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidateCountFields(string field)
    {
        if (field == "delayHours")
        {
            yield return "delayHours";
            yield return "maxDelayHours";
            yield return "longestDelayHours";
            yield break;
        }

        yield return "totalCount";
        yield return "count";
        yield return field;
    }

    private static IEnumerable<string> CandidateDecimalFields(string field)
    {
        yield return field;
        yield return "oee";
        yield return "availability";
        yield return "performance";
        yield return "quality";
    }

    private static bool TryFindNumber(JsonElement element, string propertyName, out decimal value)
    {
        value = default;

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (property.NameEquals(propertyName) && TryGetDecimal(property.Value, out value))
                {
                    return true;
                }

                if (TryFindNumber(property.Value, propertyName, out value))
                {
                    return true;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                if (TryFindNumber(item, propertyName, out value))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryGetDecimal(JsonElement value, out decimal result)
    {
        result = default;

        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.TryGetDecimal(out result);
        }

        return value.ValueKind == JsonValueKind.String &&
            TryParseDecimal(value.GetString() ?? string.Empty, out result);
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        return decimal.TryParse(
            value,
            NumberStyles.Float | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            out result);
    }

    private static bool NumbersMatch(decimal actualValue, decimal claimedValue)
    {
        return Math.Abs(actualValue - claimedValue) <= NumericTolerance;
    }

    private static string InferField(string context, string suffix)
    {
        if (IsCountSuffix(suffix))
        {
            return "totalCount";
        }

        if (IsHourSuffix(suffix))
        {
            return "delayHours";
        }

        if (context.Contains("OEE", StringComparison.OrdinalIgnoreCase))
        {
            return "oee";
        }

        if (ContainsAny(context, "availability", "available"))
        {
            return "availability";
        }

        if (context.Contains("performance", StringComparison.OrdinalIgnoreCase))
        {
            return "performance";
        }

        if (context.Contains("quality", StringComparison.OrdinalIgnoreCase))
        {
            return "quality";
        }

        if (ContainsAny(context, "work order", "work orders", "order", "orders", "item", "items"))
        {
            return "totalCount";
        }

        if (ContainsAny(context, "delay", "late", "hour", "hours", "hrs"))
        {
            return "delayHours";
        }

        if (IsPercentageSuffix(suffix))
        {
            return "percentage";
        }

        return "totalCount";
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsPercentageSuffix(string suffix)
    {
        return suffix.Equals("%", StringComparison.OrdinalIgnoreCase) ||
            suffix.Equals("percent", StringComparison.OrdinalIgnoreCase) ||
            suffix.Equals("percentage", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCountSuffix(string suffix)
    {
        return suffix.Contains("order", StringComparison.OrdinalIgnoreCase) ||
            suffix.Equals("items", StringComparison.OrdinalIgnoreCase) ||
            suffix.Equals("item", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHourSuffix(string suffix)
    {
        return suffix.Equals("hour", StringComparison.OrdinalIgnoreCase) ||
            suffix.Equals("hours", StringComparison.OrdinalIgnoreCase) ||
            suffix.Equals("hrs", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.################", CultureInfo.InvariantCulture);
    }

    private static string GetSurroundingText(string text, int position, int radius)
    {
        int start = Math.Max(0, position - radius);
        int end = Math.Min(text.Length, position + radius);
        return text[start..end];
    }

    [GeneratedRegex(@"(?<value>[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)\s*(?<suffix>%|percent|percentage|work\s+orders?|orders?|items?|hours?|hrs?)?", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex NumberWithUnitRegex();

    private sealed record NumericalClaim(string Text, decimal Value, string Field, ClaimType Type);

    private enum ClaimType
    {
        Count,
        Percentage
    }
}
