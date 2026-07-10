using MesCopilot.Agent.Caching;
using MesCopilot.Agent.Models;

namespace MesCopilot.UnitTests.Agent.Caching;

public class InMemoryAgentResponseCacheTests
{
    [Fact]
    public async Task Cache_IsTenantScopedAndPreservesQueryCase()
    {
        InMemoryAgentResponseCache cache = new();
        FunctionCallResult result = new() { Explanation = "Tenant A answer" };

        await cache.SetAsync("tenant-a", "knowledge", "Pump Status", result, TimeSpan.FromMinutes(5), default);

        Assert.Equal("Tenant A answer", (await cache.GetAsync("tenant-a", "knowledge", "Pump Status", default))?.Explanation);
        Assert.Null(await cache.GetAsync("tenant-b", "knowledge", "Pump Status", default));
        Assert.Null(await cache.GetAsync("tenant-a", "knowledge", "pump status", default));
    }

    [Fact]
    public async Task InvalidateScopeAsync_ExpiresExistingEntriesWithoutScanning()
    {
        InMemoryAgentResponseCache cache = new();
        await cache.SetAsync("tenant-a", "knowledge", "query", new FunctionCallResult(), TimeSpan.FromMinutes(5), default);

        await cache.InvalidateScopeAsync("tenant-a", "knowledge", default);

        Assert.Null(await cache.GetAsync("tenant-a", "knowledge", "query", default));
    }

    [Fact]
    public async Task Cache_EvictsOldestEntryAfterOneThousandItems()
    {
        InMemoryAgentResponseCache cache = new();
        for (int index = 0; index <= 1000; index++)
        {
            await cache.SetAsync("tenant-a", "knowledge", $"query-{index:0000}", new FunctionCallResult(), TimeSpan.FromMinutes(5), default);
        }

        Assert.Null(await cache.GetAsync("tenant-a", "knowledge", "query-0000", default));
        Assert.NotNull(await cache.GetAsync("tenant-a", "knowledge", "query-1000", default));
    }
}
