using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Entities.Quality;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Application.Services;

public class QualityRootCauseService : IQualityRootCauseService
{
    private readonly MesDbContext _context;

    public QualityRootCauseService(MesDbContext context)
    {
        _context = context;
    }

    public async Task<FiveWhyAnalysisDto> AnalyzeAsync(int? defectRecordId, string? symptomDescription)
    {
        if (!defectRecordId.HasValue)
        {
            string symptom = string.IsNullOrWhiteSpace(symptomDescription)
                ? "Unspecified quality symptom"
                : symptomDescription.Trim();
            return BuildSymptomOnlyAnalysis(symptom);
        }

        DefectRecord defect = await _context.DefectRecords
            .AsNoTracking()
            .Include(record => record.DefectType)
            .Include(record => record.QualityInspection)
            .FirstOrDefaultAsync(record => record.Id == defectRecordId.Value)
            ?? throw new InvalidOperationException($"Defect record {defectRecordId.Value} not found.");

        List<WhyLevelDto> chain =
        [
            new WhyLevelDto(
                1,
                "Why did the defect appear?",
                $"{defect.DefectType.Name}: {defect.Description ?? "No detailed description"}",
                $"Defect record {defect.Id}, quantity {defect.Quantity}"),
            new WhyLevelDto(
                2,
                "Why was the defect detected at inspection?",
                $"Inspection {defect.QualityInspection.Code} failed {defect.QualityInspection.FailedQuantity} units.",
                $"Batch {defect.QualityInspection.BatchNumber}, inspector {defect.QualityInspection.InspectorName}"),
            new WhyLevelDto(
                3,
                "Why could the process produce this condition?",
                "The current MES evidence points to process drift or operator variation.",
                $"Process step {defect.QualityInspection.ProcessStepId}"),
            new WhyLevelDto(
                4,
                "Why was the drift not contained earlier?",
                "Process control checks did not catch the issue before final inspection.",
                "Review in-process inspection cadence"),
            new WhyLevelDto(
                5,
                "Why does the control plan allow this escape?",
                "The control plan likely needs tighter trigger thresholds or clearer SOP checks.",
                "Requires engineering review")
        ];

        return new FiveWhyAnalysisDto
        {
            DefectRecordId = defect.Id,
            WhyChain = chain,
            RootCause = chain[^1].Answer,
            CorrectiveActions =
            [
                "Review the control plan thresholds for the affected process step.",
                "Add in-process inspection before final quality gate.",
                "Retrain operators on the relevant SOP checkpoint."
            ]
        };
    }

    private static FiveWhyAnalysisDto BuildSymptomOnlyAnalysis(string symptom)
    {
        List<WhyLevelDto> chain =
        [
            new WhyLevelDto(1, "Why did the issue appear?", symptom, "User supplied symptom"),
            new WhyLevelDto(2, "Why is more evidence needed?", "No defect record is linked yet.", "Missing MES defect id")
        ];

        return new FiveWhyAnalysisDto
        {
            WhyChain = chain,
            RootCause = "Root cause is not confirmed without a defect record.",
            CorrectiveActions =
            [
                "Link the symptom to a defect record or batch number.",
                "Collect inspection, equipment, material, and operator evidence."
            ]
        };
    }
}
