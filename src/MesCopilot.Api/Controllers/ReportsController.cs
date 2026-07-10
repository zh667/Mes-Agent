using MesCopilot.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesCopilot.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string PdfContentType = "application/pdf";

    private readonly IReportExportService _reportExportService;

    public ReportsController(IReportExportService reportExportService)
    {
        _reportExportService = reportExportService;
    }

    [HttpGet("production")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportProduction(
        [FromQuery] DateTime date,
        [FromQuery] int? lineId,
        [FromQuery] string format = "excel")
    {
        if (!TryParseFormat(format, out ReportFormat reportFormat))
        {
            return BadRequest(new { error = "Format must be 'excel' or 'pdf'." });
        }

        byte[] file = await _reportExportService.ExportProductionReportAsync(date == default ? DateTime.UtcNow.Date : date, lineId, reportFormat);
        return File(file, GetContentType(reportFormat), $"production-report-{DateTime.UtcNow:yyyyMMddHHmmss}.{GetExtension(reportFormat)}");
    }

    [HttpGet("quality")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportQuality(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string format = "excel")
    {
        if (!TryParseFormat(format, out ReportFormat reportFormat))
        {
            return BadRequest(new { error = "Format must be 'excel' or 'pdf'." });
        }

        DateTime start = from == default ? DateTime.UtcNow.Date : from;
        DateTime end = to == default ? start : to;
        byte[] file = await _reportExportService.ExportQualityReportAsync(start, end, reportFormat);
        return File(file, GetContentType(reportFormat), $"quality-report-{DateTime.UtcNow:yyyyMMddHHmmss}.{GetExtension(reportFormat)}");
    }

    [HttpGet("oee")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportOee(
        [FromQuery] int[] equipmentIds,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string format = "excel")
    {
        if (!TryParseFormat(format, out ReportFormat reportFormat))
        {
            return BadRequest(new { error = "Format must be 'excel' or 'pdf'." });
        }

        DateTime start = from == default ? DateTime.UtcNow.Date : from;
        DateTime end = to == default ? start : to;
        try
        {
            byte[] file = await _reportExportService.ExportOeeReportAsync(equipmentIds, start, end, reportFormat);
            return File(file, GetContentType(reportFormat), $"oee-report-{DateTime.UtcNow:yyyyMMddHHmmss}.{GetExtension(reportFormat)}");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private static bool TryParseFormat(string format, out ReportFormat reportFormat)
    {
        if (string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            reportFormat = ReportFormat.Excel;
            return true;
        }

        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            reportFormat = ReportFormat.Pdf;
            return true;
        }

        reportFormat = ReportFormat.Excel;
        return false;
    }

    private static string GetContentType(ReportFormat format)
    {
        return format == ReportFormat.Excel ? ExcelContentType : PdfContentType;
    }

    private static string GetExtension(ReportFormat format)
    {
        return format == ReportFormat.Excel ? "xlsx" : "pdf";
    }
}
