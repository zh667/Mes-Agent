using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;
using MesCopilot.Agent.Prompts;
using MesCopilot.Agent.Verification;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.Agent;

public static class DependencyInjection
{
    public static IServiceCollection AddMesCopilotAgent(this IServiceCollection services)
    {
        services.AddSingleton<IPromptBuilder, MesPromptBuilder>();
        services.AddScoped<IFactVerifier, FactVerifier>();
        services.AddScoped<IRagAnswerGenerator, VerifiedRagAnswerGenerator>();
        return services;
    }
}
