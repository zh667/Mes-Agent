using MesCopilot.Agent.Models;

namespace MesCopilot.Agent.Caching;

public interface IAgentResponseCache
{
    bool TryGet(string key, out FunctionCallResult result);

    void Set(string key, FunctionCallResult result, TimeSpan duration);
}
