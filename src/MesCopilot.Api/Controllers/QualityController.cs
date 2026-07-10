using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QualityController : ControllerBase
{
    private readonly IQualityService _qualityService;

    public QualityController(IQualityService qualityService)
    {
        _qualityService = qualityService;
    }

    [HttpGet("inspections")]
    public async Task<ActionResult<IEnumerable<QualityInspectionDto>>> GetInspections()
    {
        return Ok(await _qualityService.GetInspectionsAsync());
    }

    [HttpPost("inspections")]
    public async Task<ActionResult<QualityInspectionDto>> CreateInspection([FromBody] CreateQualityInspectionRequest request)
    {
        var inspection = await _qualityService.CreateInspectionAsync(request);
        return CreatedAtAction(nameof(TraceBatch), new { batchNumber = inspection.BatchNumber }, inspection);
    }

    [HttpGet("trace/{batchNumber}")]
    public async Task<ActionResult<BatchTraceDto>> TraceBatch(string batchNumber)
    {
        return Ok(await _qualityService.TraceBatchAsync(batchNumber));
    }

    [HttpGet("defects")]
    public async Task<ActionResult<IEnumerable<DefectAnalysisDto>>> AnalyzeDefects()
    {
        return Ok(await _qualityService.AnalyzeDefectsAsync());
    }
}
