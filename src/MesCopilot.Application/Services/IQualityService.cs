using MesCopilot.Application.Dtos;

namespace MesCopilot.Application.Services;

public interface IQualityService
{
    Task<IEnumerable<QualityInspectionDto>> GetInspectionsAsync();

    Task<QualityInspectionDto> CreateInspectionAsync(CreateQualityInspectionRequest request);

    Task<BatchTraceDto> TraceBatchAsync(string batchNumber);

    Task<IEnumerable<DefectAnalysisDto>> AnalyzeDefectsAsync();
}
