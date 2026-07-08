using MesCopilot.Agent.Plugins.QualityAgentPlugin.Tools;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.QualityAgentPlugin;

public class QualityAgentPlugin
{
    public QualityAgentPlugin(IQualityService qualityService)
    {
        TraceBatchTool = new TraceBatchTool(qualityService);
        AnalyzeDefectPatternTool = new AnalyzeDefectPatternTool(qualityService);
        GetDefectsByProcessTool = new GetDefectsByProcessTool(qualityService);
    }

    public TraceBatchTool TraceBatchTool { get; }

    public AnalyzeDefectPatternTool AnalyzeDefectPatternTool { get; }

    public GetDefectsByProcessTool GetDefectsByProcessTool { get; }

    public string Name => "QualityAgent";

    public string Description => "Quality agent for batch tracing and defect analysis.";
}
