using MesCopilot.Agent.Models;

namespace MesCopilot.Agent.Caching;

public interface IAgentResponseCache
{
    Task<FunctionCallResult?> GetAsync(
        string tenantId,
        string scope,
        string key,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        string tenantId,
        string scope,
        string key,
        FunctionCallResult value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    Task InvalidateScopeAsync(
        string tenantId,
        string scope,
        CancellationToken cancellationToken = default);
}
