namespace MesCopilot.Application.Dtos;

public class MaintenancePredictionDto
{
    public int EquipmentId { get; set; }

    public string EquipmentCode { get; set; } = string.Empty;

    public string EquipmentName { get; set; } = string.Empty;

    public double HealthScore { get; set; }

    public string HealthLevel { get; set; } = string.Empty;

    public double MtbfHours { get; set; }

    public double MttrHours { get; set; }

    public DateTime? PredictedNextFailureAt { get; set; }

    public string MaintenanceRecommendation { get; set; } = string.Empty;

    public IReadOnlyList<RecentFailureDto> RecentFailures { get; set; } = [];

    public TrendDto Trend { get; set; } = new();
}

public record RecentFailureDto(
    DateTime OccurredAt,
    string Reason,
    double DurationMinutes);

public class TrendDto
{
    public string Direction { get; set; } = "Stable";

    public double ChangePercent { get; set; }

    public string Description { get; set; } = "Not enough data to determine trend.";
}
