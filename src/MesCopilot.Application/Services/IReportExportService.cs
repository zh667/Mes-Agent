namespace MesCopilot.Application.Services;

public enum ReportFormat
{
    Excel,
    Pdf
}

public interface IReportExportService
{
    Task<byte[]> ExportProductionReportAsync(DateTime date, int? lineId, ReportFormat format);

    Task<byte[]> ExportQualityReportAsync(DateTime from, DateTime to, ReportFormat format);

    Task<byte[]> ExportOeeReportAsync(IReadOnlyList<int> equipmentIds, DateTime from, DateTime to, ReportFormat format);
}
