using MesCopilot.Agent.Plugins.ProductionAgentPlugin.Tools;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.ProductionAgentPlugin;

public class ProductionAgentPlugin
{
    public ProductionAgentPlugin(IWorkOrderService workOrderService)
    {
        GetTodayWorkOrdersTool = new GetTodayWorkOrdersTool(workOrderService);
        AnalyzeDelayedOrdersTool = new AnalyzeDelayedOrdersTool(workOrderService);
    }

    public GetTodayWorkOrdersTool GetTodayWorkOrdersTool { get; }

    public AnalyzeDelayedOrdersTool AnalyzeDelayedOrdersTool { get; }

    public string Name => "ProductionAgent";

    public string Description => "Production operations agent for querying work orders and analyzing delays.";
}
