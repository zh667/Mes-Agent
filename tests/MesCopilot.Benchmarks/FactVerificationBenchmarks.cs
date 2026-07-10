using BenchmarkDotNet.Attributes;
using MesCopilot.Agent.Models;
using MesCopilot.Agent.Verification;

namespace MesCopilot.Benchmarks;

[MemoryDiagnoser]
public class FactVerificationBenchmarks
{
    private readonly FactVerifier _verifier = new();
    private FunctionCallResult _result = null!;

    [GlobalSetup]
    public void Setup()
    {
        _result = new FunctionCallResult
        {
            Data = new { totalCount = 50, delayHours = 8, oee = 82.5m, availability = 91.2m },
            Explanation = "There are 50 work orders, the longest delay is 8 hours, OEE is 82.5%, and availability is 91.2%."
        };
    }

    [Benchmark]
    public VerificationResult VerifyStructuredClaims() => _verifier.Verify(_result);
}
