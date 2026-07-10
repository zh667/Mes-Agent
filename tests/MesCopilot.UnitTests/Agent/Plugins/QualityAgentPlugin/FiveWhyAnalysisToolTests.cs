using MesCopilot.Agent.Plugins.QualityAgentPlugin.Tools;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;

namespace MesCopilot.UnitTests.Agent.Plugins.QualityAgentPluginTools;

public class FiveWhyAnalysisToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithSymptomDescription_ReturnsFiveWhyChain()
    {
        FiveWhyAnalysisTool tool = new(new FakeQualityRootCauseService());

        var result = await tool.ExecuteAsync(defectRecordId: null, symptomDescription: "Surface scratch", debugMode: true);

        Assert.NotNull(result.Data);
        Assert.Contains("Surface scratch", result.Explanation);
        Assert.Equal("FiveWhyAnalysis", result.Debug?.ToolsCalled?.Single());
    }

    [Fact]
    public async Task ExecuteAsync_WithoutDefectOrSymptom_ThrowsArgumentException()
    {
        FiveWhyAnalysisTool tool = new(new FakeQualityRootCauseService());

        await Assert.ThrowsAsync<ArgumentException>(() => tool.ExecuteAsync(null, " "));
    }

    private sealed class FakeQualityRootCauseService : IQualityRootCauseService
    {
        public Task<FiveWhyAnalysisDto> AnalyzeAsync(int? defectRecordId, string? symptomDescription)
        {
            return Task.FromResult(new FiveWhyAnalysisDto
            {
                DefectRecordId = defectRecordId,
                RootCause = "Operator skipped surface cleaning",
                CorrectiveActions =
                [
                    "Retrain operator on SOP",
                    "Add first-piece inspection"
                ],
                WhyChain =
                [
                    new WhyLevelDto(1, "Why did the defect appear?", symptomDescription ?? "Defect record", "Operator report"),
                    new WhyLevelDto(2, "Why was cleaning skipped?", "SOP step was missed", "Shift checklist")
                ]
            });
        }
    }
}
