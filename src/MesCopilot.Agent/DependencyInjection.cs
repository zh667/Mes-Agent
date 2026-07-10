using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;
using MesCopilot.Agent.Prompts;
using MesCopilot.Agent.Verification;
using MesCopilot.Agent.Verification.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.Agent;

public static class DependencyInjection
{
    public static IServiceCollection AddMesCopilotAgent(this IServiceCollection services)
    {
        services.AddSingleton<IPromptBuilder, MesPromptBuilder>();
        services.AddScoped<IVerificationRule, DelayedOrdersVerificationRule>();
        services.AddScoped<IVerificationRule, OeeVerificationRule>();
        services.AddScoped<IVerificationRule, QualityVerificationRule>();
        services.AddScoped<IVerificationRule, KnowledgeCitationVerificationRule>();
        services.AddScoped<IFactVerifier, FactVerifier>();
        services.AddScoped<IRagAnswerGenerator, VerifiedRagAnswerGenerator>();
        return services;
    }
}
