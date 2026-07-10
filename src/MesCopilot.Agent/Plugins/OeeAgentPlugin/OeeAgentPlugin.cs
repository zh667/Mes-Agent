using MesCopilot.Agent.Plugins.OeeAgentPlugin.Tools;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.OeeAgentPlugin;

public class OeeAgentPlugin
{
    public OeeAgentPlugin(IEquipmentService equipmentService)
        : this(equipmentService, null)
    {
    }

    public OeeAgentPlugin(
        IEquipmentService equipmentService,
        IMaintenancePredictionService? maintenancePredictionService)
    {
        CalculateOeeTool = new CalculateOeeTool(equipmentService);
        AnalyzeLowOeeTool = new AnalyzeLowOeeTool(equipmentService);
        GetEquipmentStatusTool = new GetEquipmentStatusTool(equipmentService);
        PredictMaintenanceTool = maintenancePredictionService is null
            ? null
            : new PredictMaintenanceTool(maintenancePredictionService);
    }

    public CalculateOeeTool CalculateOeeTool { get; }

    public AnalyzeLowOeeTool AnalyzeLowOeeTool { get; }

    public GetEquipmentStatusTool GetEquipmentStatusTool { get; }

    public PredictMaintenanceTool? PredictMaintenanceTool { get; }

    public string Name => "OeeAgent";

    public string Description => "OEE agent for equipment efficiency and status analysis.";
}
