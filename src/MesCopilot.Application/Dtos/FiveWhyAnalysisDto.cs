namespace MesCopilot.Application.Dtos;

public class FiveWhyAnalysisDto
{
    public int? DefectRecordId { get; set; }

    public IReadOnlyList<WhyLevelDto> WhyChain { get; set; } = [];

    public string RootCause { get; set; } = string.Empty;

    public IReadOnlyList<string> CorrectiveActions { get; set; } = [];
}

public record WhyLevelDto(
    int Level,
    string Question,
    string Answer,
    string Evidence);
