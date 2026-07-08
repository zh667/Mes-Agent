using MesCopilot.Agent.Plugins.OeeAgentPlugin.Tools;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.OeeAgentPlugin;

public class OeeAgentPlugin
{
    public OeeAgentPlugin(IEquipmentService equipmentService)
    {
        CalculateOeeTool = new CalculateOeeTool(equipmentService);
        AnalyzeLowOeeTool = new AnalyzeLowOeeTool(equipmentService);
        GetEquipmentStatusTool = new GetEquipmentStatusTool(equipmentService);
    }

    public CalculateOeeTool CalculateOeeTool { get; }

    public AnalyzeLowOeeTool AnalyzeLowOeeTool { get; }

    public GetEquipmentStatusTool GetEquipmentStatusTool { get; }

    public string Name => "OeeAgent";

    public string Description => "OEE agent for equipment efficiency and status analysis.";
}
