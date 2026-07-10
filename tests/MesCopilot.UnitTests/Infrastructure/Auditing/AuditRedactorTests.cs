using System.Text;
using MesCopilot.Infrastructure.Auditing;

namespace MesCopilot.UnitTests.Infrastructure.Auditing;

public class AuditRedactorTests
{
    [Fact]
    public void RedactJson_RedactsSensitiveKeysAtEveryDepth()
    {
        AuditRedactor redactor = new("audit-hash-key");

        string result = redactor.RedactJson("""
            {"user":"operator","password":"secret","nested":{"accessToken":"abc","value":42}}
            """)!;

        Assert.DoesNotContain("secret", result, StringComparison.Ordinal);
        Assert.DoesNotContain("abc", result, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", result, StringComparison.Ordinal);
        Assert.Contains("operator", result, StringComparison.Ordinal);
    }

    [Fact]
    public void RedactJson_CapsUtf8PayloadAtSixteenKiB()
    {
        AuditRedactor redactor = new("audit-hash-key");
        string json = $$"""{"description":"{{new string('x', 32 * 1024)}}"}""";

        string result = redactor.RedactJson(json)!;

        Assert.True(Encoding.UTF8.GetByteCount(result) <= AuditRedactor.MaxPayloadBytes);
    }

    [Fact]
    public void HashIp_ReturnsStableDigestWithoutRawAddress()
    {
        AuditRedactor redactor = new("audit-hash-key");

        string? first = redactor.HashIp("192.0.2.10");
        string? second = redactor.HashIp("192.0.2.10");

        Assert.Equal(first, second);
        Assert.DoesNotContain("192.0.2.10", first, StringComparison.Ordinal);
    }
}
