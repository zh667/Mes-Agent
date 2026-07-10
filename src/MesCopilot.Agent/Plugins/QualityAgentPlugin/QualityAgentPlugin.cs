using MesCopilot.Agent.Plugins.QualityAgentPlugin.Tools;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.QualityAgentPlugin;

public class QualityAgentPlugin
{
    public QualityAgentPlugin(IQualityService qualityService)
        : this(qualityService, null)
    {
    }

    public QualityAgentPlugin(
        IQualityService qualityService,
        IQualityRootCauseService? rootCauseService)
    {
        TraceBatchTool = new TraceBatchTool(qualityService);
        AnalyzeDefectPatternTool = new AnalyzeDefectPatternTool(qualityService);
        GetDefectsByProcessTool = new GetDefectsByProcessTool(qualityService);
        FiveWhyAnalysisTool = rootCauseService is null
            ? null
            : new FiveWhyAnalysisTool(rootCauseService);
    }

    public TraceBatchTool TraceBatchTool { get; }

    public AnalyzeDefectPatternTool AnalyzeDefectPatternTool { get; }

    public GetDefectsByProcessTool GetDefectsByProcessTool { get; }

    public FiveWhyAnalysisTool? FiveWhyAnalysisTool { get; }

    public string Name => "QualityAgent";

    public string Description => "Quality agent for batch tracing and defect analysis.";
}
