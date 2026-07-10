using MesCopilot.Application.Dtos;

namespace MesCopilot.Application.Services;

public interface IQualityRootCauseService
{
    Task<FiveWhyAnalysisDto> AnalyzeAsync(int? defectRecordId, string? symptomDescription);
}
