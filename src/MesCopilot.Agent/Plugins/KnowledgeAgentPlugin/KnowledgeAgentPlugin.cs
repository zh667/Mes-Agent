using MesCopilot.Agent.Caching;
using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin.Tools;
using MesCopilot.Application.Services;
using MesCopilot.Infrastructure.Tenancy;

namespace MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;

public class KnowledgeAgentPlugin
{
    public KnowledgeAgentPlugin(IKnowledgeService knowledgeService)
        : this(knowledgeService, null)
    {
    }

    public KnowledgeAgentPlugin(
        IKnowledgeService knowledgeService,
        IAgentResponseCache? responseCache)
        : this(knowledgeService, responseCache, null)
    {
    }

    public KnowledgeAgentPlugin(
        IKnowledgeService knowledgeService,
        IAgentResponseCache? responseCache,
        ITenantContext? tenantContext)
    {
        SearchDocumentsTool = new SearchDocumentsTool(
            knowledgeService,
            new VerifiedRagAnswerGenerator(),
            responseCache,
            tenantContext);
        GetSopByCodeTool = new GetSopByCodeTool(knowledgeService);
    }

    public SearchDocumentsTool SearchDocumentsTool { get; }

    public GetSopByCodeTool GetSopByCodeTool { get; }

    public string Name => "KnowledgeAgent";

    public string Description => "Knowledge agent for document search and SOP lookup.";
}
