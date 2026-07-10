using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MesCopilot.Infrastructure.Auditing;

public sealed class AuditRedactor
{
    public const int MaxPayloadBytes = 16 * 1024;

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "secret", "token", "accessToken", "refreshToken", "authorization", "apiKey",
        "connectionString", "certificate", "privateKey"
    };

    private readonly byte[] _ipHashKey;

    public AuditRedactor(string ipHashKey)
    {
        _ipHashKey = Encoding.UTF8.GetBytes(ipHashKey);
    }

    public string? RedactJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        string sanitized;
        try
        {
            JsonNode? node = JsonNode.Parse(json);
            RedactNode(node);
            sanitized = node?.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? "null";
        }
        catch (JsonException)
        {
            sanitized = "[UNPARSEABLE JSON]";
        }

        return TruncateUtf8(sanitized, MaxPayloadBytes);
    }

    public string RedactValues(IReadOnlyDictionary<string, object?> values)
    {
        return RedactJson(JsonSerializer.Serialize(values)) ?? "{}";
    }

    public string? HashIp(string? address)
    {
        if (string.IsNullOrWhiteSpace(address) || _ipHashKey.Length == 0)
        {
            return null;
        }

        using HMACSHA256 hmac = new(_ipHashKey);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(address)));
    }

    private static void RedactNode(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (string key in jsonObject.Select(item => item.Key).ToList())
            {
                if (SensitiveKeys.Contains(key))
                {
                    jsonObject[key] = "[REDACTED]";
                }
                else
                {
                    RedactNode(jsonObject[key]);
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (JsonNode? item in jsonArray)
            {
                RedactNode(item);
            }
        }
    }

    private static string TruncateUtf8(string value, int maxBytes)
    {
        if (Encoding.UTF8.GetByteCount(value) <= maxBytes)
        {
            return value;
        }

        const string suffix = "...";
        int byteBudget = maxBytes - Encoding.UTF8.GetByteCount(suffix);
        int length = value.Length;
        while (length > 0 && Encoding.UTF8.GetByteCount(value.AsSpan(0, length)) > byteBudget)
        {
            length--;
        }

        return string.Concat(value.AsSpan(0, length), suffix);
    }
}
