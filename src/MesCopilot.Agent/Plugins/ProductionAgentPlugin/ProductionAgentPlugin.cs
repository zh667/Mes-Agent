using MesCopilot.Agent.Plugins.ProductionAgentPlugin.Tools;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.ProductionAgentPlugin;

public class ProductionAgentPlugin
{
    public ProductionAgentPlugin(IWorkOrderService workOrderService)
        : this(workOrderService, null)
    {
    }

    public ProductionAgentPlugin(
        IWorkOrderService workOrderService,
        IEquipmentService? equipmentService)
    {
        GetTodayWorkOrdersTool = new GetTodayWorkOrdersTool(workOrderService);
        AnalyzeDelayedOrdersTool = new AnalyzeDelayedOrdersTool(workOrderService);
        SuggestScheduleTool = equipmentService is null
            ? null
            : new SuggestScheduleTool(workOrderService, equipmentService);
    }

    public GetTodayWorkOrdersTool GetTodayWorkOrdersTool { get; }

    public AnalyzeDelayedOrdersTool AnalyzeDelayedOrdersTool { get; }

    public SuggestScheduleTool? SuggestScheduleTool { get; }

    public string Name => "ProductionAgent";

    public string Description => "Production operations agent for querying work orders and analyzing delays.";
}
