using MesCopilot.Agent.Plugins.QualityAgentPlugin;
using MesCopilot.Agent.Plugins.QualityAgentPlugin.Tools;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Agent.Plugins;

public class QualityAgentPluginTests
{
    [Fact]
    public async Task TraceBatchTool_ShouldReturnTraceSummary()
    {
        var service = new FakeQualityService();
        var tool = new TraceBatchTool(service);

        var result = await tool.ExecuteAsync("B-001", debugMode: true);

        Assert.NotNull(result.Data);
        Assert.Contains("B-001", result.Explanation);
        Assert.Equal("TraceBatch", result.Debug?.ToolsCalled?.Single());
    }

    [Fact]
    public async Task TraceBatchTool_EmptyBatchNumber_ShouldThrow()
    {
        var tool = new TraceBatchTool(new FakeQualityService());

        await Assert.ThrowsAsync<ArgumentException>(() => tool.ExecuteAsync(""));
    }

    [Fact]
    public async Task AnalyzeDefectPatternTool_ShouldReturnDefectSummary()
    {
        var service = new FakeQualityService();
        var tool = new AnalyzeDefectPatternTool(service);

        var result = await tool.ExecuteAsync(debugMode: true);

        Assert.NotNull(result.Data);
        Assert.Contains("Scratch", result.Explanation);
        Assert.Equal("AnalyzeDefectPattern", result.Debug?.ToolsCalled?.Single());
    }

    [Fact]
    public async Task GetDefectsByProcessTool_ShouldReturnProcessScopedSummary()
    {
        var service = new FakeQualityService();
        var tool = new GetDefectsByProcessTool(service);

        var result = await tool.ExecuteAsync(7, debugMode: true);

        Assert.NotNull(result.Data);
        Assert.Contains("7", result.Explanation);
        Assert.Equal("GetDefectsByProcess", result.Debug?.ToolsCalled?.Single());
    }

    [Fact]
    public async Task GetDefectsByProcessTool_InvalidProcessStepId_ShouldThrow()
    {
        var tool = new GetDefectsByProcessTool(new FakeQualityService());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => tool.ExecuteAsync(0));
    }

    [Fact]
    public void QualityAgentPlugin_ShouldExposeQualityTools()
    {
        var plugin = new QualityAgentPlugin(new FakeQualityService());

        Assert.Equal("QualityAgent", plugin.Name);
        Assert.NotNull(plugin.TraceBatchTool);
        Assert.NotNull(plugin.AnalyzeDefectPatternTool);
        Assert.NotNull(plugin.GetDefectsByProcessTool);
    }

    private sealed class FakeQualityService : IQualityService
    {
        public Task<IEnumerable<QualityInspectionDto>> GetInspectionsAsync()
        {
            return Task.FromResult<IEnumerable<QualityInspectionDto>>(
            [
                new QualityInspectionDto(1, "QI-001", "B-001", 1, 7, "qc-1", "QC One", 10, 9, 1, InspectionStatus.Fail, DateTime.UtcNow, "Scratch")
            ]);
        }

        public Task<QualityInspectionDto> CreateInspectionAsync(CreateQualityInspectionRequest request)
        {
            throw new NotSupportedException();
        }

        public Task<BatchTraceDto> TraceBatchAsync(string batchNumber)
        {
            var report = new ProductionReportTraceDto(1, batchNumber, 1, 7, 2, "op-1", "Operator", DateTime.UtcNow, 10, 9);
            var inspection = new QualityInspectionDto(1, "QI-001", batchNumber, 1, 7, "qc-1", "QC One", 10, 9, 1, InspectionStatus.Fail, DateTime.UtcNow, "Scratch");
            return Task.FromResult(new BatchTraceDto(batchNumber, [report], [inspection]));
        }

        public Task<IEnumerable<DefectAnalysisDto>> AnalyzeDefectsAsync()
        {
            return Task.FromResult<IEnumerable<DefectAnalysisDto>>(
            [
                new DefectAnalysisDto(1, "D-001", "Scratch", 5)
            ]);
        }
    }
}
