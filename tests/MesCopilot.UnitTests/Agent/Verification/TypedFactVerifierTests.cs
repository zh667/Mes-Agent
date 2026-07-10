using MesCopilot.Agent.Models;
using MesCopilot.Agent.Verification;
using MesCopilot.Agent.Verification.Rules;
using MesCopilot.Application.Services.Verification;
using Microsoft.Extensions.Logging;

namespace MesCopilot.UnitTests.Agent.Verification;

public sealed class TypedFactVerifierTests
{
    [Fact]
    public async Task VerifyAsync_WhenDelayedOrderCountMatches_ReturnsVerified()
    {
        FakeVerificationQueryService queries = new() { DelayedOrderCount = 2 };
        FactVerifier verifier = CreateVerifier(queries);

        VerificationResult result = await verifier.VerifyAsync(
            new FunctionCallResult
            {
                Data = new { totalCount = 2, workOrders = new[] { new { id = 1 }, new { id = 2 } } },
                Explanation = "There are 2 delayed work orders."
            },
            CreateContext("AnalyzeDelayedOrders"));

        Assert.Equal(VerificationStatus.Verified, result.Status);
        Assert.Single(result.Checks);
        Assert.Equal("2", result.Checks[0].ActualValue);
    }

    [Fact]
    public async Task VerifyAsync_WhenOeeDiffers_ReturnsDisputed()
    {
        FakeVerificationQueryService queries = new() { Oee = 0.80m };
        FactVerifier verifier = CreateVerifier(queries);

        VerificationResult result = await verifier.VerifyAsync(
            new FunctionCallResult
            {
                Data = new { equipmentId = 7, date = new DateTime(2026, 7, 10), oee = 0.82m },
                Explanation = "Equipment 7 OEE is 82%."
            },
            CreateContext("CalculateOee"));

        Assert.Equal(VerificationStatus.Disputed, result.Status);
        Assert.Equal("0.82", Assert.Single(result.Checks).ClaimedValue);
        Assert.Equal("0.8", result.Checks[0].ActualValue);
    }

    [Fact]
    public async Task VerifyAsync_WhenNoRuleSupportsTool_ReturnsUnverified()
    {
        FactVerifier verifier = CreateVerifier(new FakeVerificationQueryService());

        VerificationResult result = await verifier.VerifyAsync(
            new FunctionCallResult { Data = new { value = 1 }, Explanation = "Value is 1." },
            CreateContext("UnknownTool"));

        Assert.Equal(VerificationStatus.Unverified, result.Status);
        Assert.Equal("VERIFICATION_RULE_NOT_FOUND", result.ErrorCode);
        Assert.Empty(result.Checks);
    }

    [Fact]
    public async Task VerifyAsync_WhenPrimaryQueryFails_ReturnsUnverified()
    {
        FakeVerificationQueryService queries = new() { Exception = new InvalidOperationException("database unavailable") };
        FactVerifier verifier = CreateVerifier(queries);

        VerificationResult result = await verifier.VerifyAsync(
            new FunctionCallResult
            {
                Data = new { totalCount = 1, workOrders = new[] { new { id = 1 } } },
                Explanation = "There is 1 delayed work order."
            },
            CreateContext("AnalyzeDelayedOrders"));

        Assert.Equal(VerificationStatus.Unverified, result.Status);
        Assert.Equal("VERIFICATION_QUERY_FAILED", result.ErrorCode);
        Assert.DoesNotContain("database unavailable", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyAsync_WhenPrimaryQueryFails_LogsCorrelationWithoutLeakingToResult()
    {
        FakeVerificationQueryService queries = new() { Exception = new InvalidOperationException("database unavailable") };
        RecordingLogger logger = new();
        FactVerifier verifier = new(
            [new DelayedOrdersVerificationRule(queries)],
            logger);

        VerificationResult result = await verifier.VerifyAsync(
            new FunctionCallResult
            {
                Data = new { totalCount = 1 },
                Explanation = "There is 1 delayed work order."
            },
            CreateContext("AnalyzeDelayedOrders"));

        Assert.Equal("VERIFICATION_QUERY_FAILED", result.ErrorCode);
        Assert.Contains(logger.Messages, message => message.Contains("correlation-a", StringComparison.Ordinal));
    }

    [Fact]
    public async Task VerifyAsync_WhenCanceled_PropagatesCancellation()
    {
        FactVerifier verifier = CreateVerifier(new FakeVerificationQueryService());
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => verifier.VerifyAsync(
            new FunctionCallResult { Data = new { totalCount = 1 }, Explanation = "1 delayed work order" },
            CreateContext("AnalyzeDelayedOrders"),
            cancellation.Token));
    }

    [Fact]
    public async Task VerifyAsync_WhenBatchTraceCountsMatch_ReturnsVerified()
    {
        FakeVerificationQueryService queries = new()
        {
            BatchTrace = new VerifiedBatchTrace("B20260710-A", 3, 2)
        };
        FactVerifier verifier = CreateVerifier(queries);

        VerificationResult result = await verifier.VerifyAsync(
            new FunctionCallResult
            {
                Data = new { batchNumber = "B20260710-A", productionReportCount = 3, inspectionCount = 2 },
                Explanation = "Batch trace has 3 production reports and 2 inspections."
            },
            CreateContext("TraceBatch"));

        Assert.Equal(VerificationStatus.Verified, result.Status);
        Assert.Equal(2, result.Checks.Count);
    }

    [Fact]
    public async Task VerifyAsync_WhenKnowledgeCitationDoesNotExist_ReturnsDisputed()
    {
        FakeVerificationQueryService queries = new() { ExistingDocumentIds = new HashSet<int> { 10 } };
        FactVerifier verifier = CreateVerifier(queries);

        VerificationResult result = await verifier.VerifyAsync(
            new FunctionCallResult
            {
                Data = new { chunks = new[] { new { documentId = 10 }, new { documentId = 20 } } },
                Explanation = "Two sources were retrieved."
            },
            CreateContext("SearchDocuments"));

        Assert.Equal(VerificationStatus.Disputed, result.Status);
        Assert.Equal("2", result.Checks[0].ClaimedValue);
        Assert.Equal("1", result.Checks[0].ActualValue);
    }

    [Fact]
    public async Task VerifyAsync_WhenKnowledgeCitationLimitIsExceeded_DoesNotQueryDatabase()
    {
        FakeVerificationQueryService queries = new();
        FactVerifier verifier = CreateVerifier(queries);
        var chunks = Enumerable.Range(1, 101).Select(documentId => new { documentId }).ToArray();

        VerificationResult result = await verifier.VerifyAsync(
            new FunctionCallResult { Data = new { chunks }, Explanation = "Sources were retrieved." },
            CreateContext("SearchDocuments"));

        Assert.Equal(VerificationStatus.Unverified, result.Status);
        Assert.Equal("KNOWLEDGE_CITATION_LIMIT_EXCEEDED", result.ErrorCode);
        Assert.Equal(0, queries.DocumentLookupCount);
    }

    private static FactVerifier CreateVerifier(IVerificationQueryService queries)
    {
        IVerificationRule[] rules =
        [
            new DelayedOrdersVerificationRule(queries),
            new OeeVerificationRule(queries),
            new QualityVerificationRule(queries),
            new KnowledgeCitationVerificationRule(queries)
        ];
        return new FactVerifier(rules);
    }

    private static VerificationContext CreateContext(string toolName)
    {
        return new VerificationContext(
            "tenant-a",
            "user-a",
            MesCopilot.Domain.Enums.AgentMode.Production,
            toolName,
            new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc),
            "correlation-a");
    }

    private sealed class FakeVerificationQueryService : IVerificationQueryService
    {
        public int DelayedOrderCount { get; init; }

        public decimal Oee { get; init; }

        public Exception? Exception { get; init; }

        public VerifiedBatchTrace? BatchTrace { get; init; }

        public IReadOnlySet<int> ExistingDocumentIds { get; init; } = new HashSet<int>();

        public int DocumentLookupCount { get; private set; }

        public Task<int> GetDelayedOrderCountAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Exception is null ? Task.FromResult(DelayedOrderCount) : Task.FromException<int>(Exception);
        }

        public Task<VerifiedOeeSnapshot?> GetOeeAsync(int equipmentId, DateTime date, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Exception is null
                ? Task.FromResult<VerifiedOeeSnapshot?>(new VerifiedOeeSnapshot(equipmentId, date, Oee))
                : Task.FromException<VerifiedOeeSnapshot?>(Exception);
        }

        public Task<VerifiedBatchTrace?> GetBatchTraceAsync(string batchNumber, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(BatchTrace);
        }

        public Task<IReadOnlySet<int>> GetExistingDocumentIdsAsync(IReadOnlyCollection<int> documentIds, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DocumentLookupCount++;
            return Task.FromResult(ExistingDocumentIds);
        }
    }

    private sealed class RecordingLogger : ILogger<FactVerifier>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
