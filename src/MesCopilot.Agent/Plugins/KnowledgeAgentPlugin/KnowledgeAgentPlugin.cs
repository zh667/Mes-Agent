using MesCopilot.Agent.Caching;
using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin.Tools;
using MesCopilot.Application.Services;

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
    {
        SearchDocumentsTool = new SearchDocumentsTool(
            knowledgeService,
            new ContextualRagAnswerGenerator(),
            responseCache);
        GetSopByCodeTool = new GetSopByCodeTool(knowledgeService);
    }

    public SearchDocumentsTool SearchDocumentsTool { get; }

    public GetSopByCodeTool GetSopByCodeTool { get; }

    public string Name => "KnowledgeAgent";

    public string Description => "Knowledge agent for document search and SOP lookup.";
}
