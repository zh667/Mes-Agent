using MesCopilot.Application.Dtos.Auditing;
using MesCopilot.Application.Services;
using MesCopilot.Application.Services.Auditing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesCopilot.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Policy = "RequireAdmin")]
public sealed class AuditController : ControllerBase
{
    private const int MaxTake = 100;
    private const int MaxExportRows = 10_000;
    private const int MaxExportDays = 31;
    private readonly IAuditQueryService _queryService;
    private readonly IReportExportService _reportExportService;

    public AuditController(IAuditQueryService queryService, IReportExportService reportExportService)
    {
        _queryService = queryService;
        _reportExportService = reportExportService;
    }

    [HttpGet("operations")]
    public async Task<ActionResult<PagedAuditResultDto<AuditLogDto>>> GetOperations(
        DateTime? from, DateTime? to, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        if (!IsValidPage(skip, take)) return BadRequest(new { error = "skip must be non-negative and take must be between 1 and 100." });
        return Ok(await _queryService.GetOperationsAsync(from, to, skip, take, cancellationToken));
    }

    [HttpGet("changes")]
    public async Task<ActionResult<PagedAuditResultDto<DataChangeLogDto>>> GetChanges(
        DateTime? from, DateTime? to, string? entityType, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        if (!IsValidPage(skip, take)) return BadRequest(new { error = "skip must be non-negative and take must be between 1 and 100." });
        return Ok(await _queryService.GetChangesAsync(from, to, entityType, skip, take, cancellationToken));
    }

    [HttpGet("agent")]
    public async Task<ActionResult<PagedAuditResultDto<AgentAuditLogDto>>> GetAgent(
        DateTime? from, DateTime? to, string? status, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        if (!IsValidPage(skip, take)) return BadRequest(new { error = "skip must be non-negative and take must be between 1 and 100." });
        return Ok(await _queryService.GetAgentAsync(from, to, status, skip, take, cancellationToken));
    }

    [HttpGet("report")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/pdf")]
    public async Task<IActionResult> Export(
        string category,
        string format,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        int days = (to.Date - from.Date).Days + 1;
        if (days is < 1 or > MaxExportDays)
        {
            return BadRequest(new { error = $"Audit exports must cover between 1 and {MaxExportDays} days." });
        }

        if (!TryParseFormat(format, out ReportFormat reportFormat))
        {
            return BadRequest(new { error = "format must be xlsx or pdf." });
        }

        IReadOnlyList<AuditReportRowDto> rows;
        try
        {
            rows = await _queryService.GetReportRowsAsync(category, from, to, MaxExportRows + 1, cancellationToken);
        }
        catch (ArgumentException)
        {
            return BadRequest(new { error = "category must be operations, changes, or agent." });
        }

        if (rows.Count > MaxExportRows)
        {
            return BadRequest(new { error = $"Audit exports cannot exceed {MaxExportRows} records." });
        }

        byte[] content = await _reportExportService.ExportAuditReportAsync(rows, reportFormat);
        string extension = reportFormat == ReportFormat.Excel ? "xlsx" : "pdf";
        string contentType = reportFormat == ReportFormat.Excel
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "application/pdf";
        return File(content, contentType, $"audit-{category}-{from:yyyyMMdd}-{to:yyyyMMdd}.{extension}");
    }

    private static bool IsValidPage(int skip, int take) => skip >= 0 && take is >= 1 and <= MaxTake;

    private static bool TryParseFormat(string value, out ReportFormat format)
    {
        if (string.Equals(value, "xlsx", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "excel", StringComparison.OrdinalIgnoreCase))
        {
            format = ReportFormat.Excel;
            return true;
        }

        if (string.Equals(value, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            format = ReportFormat.Pdf;
            return true;
        }

        format = default;
        return false;
    }
}
