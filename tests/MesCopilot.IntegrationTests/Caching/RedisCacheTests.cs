using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MesCopilot.Agent.Caching;
using MesCopilot.Agent.Models;
using MesCopilot.Infrastructure.Caching;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.IntegrationTests.Caching;

public sealed class RedisCacheTests : IAsyncLifetime
{
    private readonly IContainer _redis = new ContainerBuilder("redis:7-alpine")
        .WithPortBinding(6379, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("redis-cli", "ping"))
        .Build();

    public Task InitializeAsync() => _redis.StartAsync();

    public Task DisposeAsync() => _redis.DisposeAsync().AsTask();

    [Fact]
    public async Task RedisAgentCache_IsTenantScopedAndSupportsVersionInvalidation()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddStackExchangeRedisCache(options =>
            options.Configuration = $"127.0.0.1:{_redis.GetMappedPublicPort(6379)},abortConnect=false");
        services.AddSingleton<ICacheScopeVersionStore, DistributedCacheScopeVersionStore>();
        services.AddSingleton<IAgentResponseCache, RedisAgentResponseCache>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IAgentResponseCache cache = provider.GetRequiredService<IAgentResponseCache>();
        FunctionCallResult value = new() { Explanation = "cached" };

        await cache.SetAsync("tenant-a", "knowledge", "Pump", value, TimeSpan.FromMinutes(5));

        Assert.Equal("cached", (await cache.GetAsync("tenant-a", "knowledge", "Pump"))?.Explanation);
        Assert.Null(await cache.GetAsync("tenant-b", "knowledge", "Pump"));
        await cache.InvalidateScopeAsync("tenant-a", "knowledge");
        Assert.Null(await cache.GetAsync("tenant-a", "knowledge", "Pump"));
    }
}
