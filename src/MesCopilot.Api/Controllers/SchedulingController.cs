using MesCopilot.Api.Dtos.Scheduling;
using MesCopilot.Application.Dtos.Scheduling;
using MesCopilot.Application.Services.Scheduling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesCopilot.Api.Controllers;

[ApiController]
[Route("api/scheduling")]
[Authorize]
public sealed class SchedulingController : ControllerBase
{
    private readonly ISchedulingService _service;

    public SchedulingController(ISchedulingService service)
    {
        _service = service;
    }

    [HttpPost("generate")]
    [Authorize(Policy = "RequireTeamLead")]
    public async Task<ActionResult<ScheduleGenerationResultDto>> Generate(
        GenerateScheduleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.GenerateAsync(request.WorkOrderIds, request.ScheduleStartUtc, cancellationToken));
        }
        catch (ScheduleValidationException exception)
        {
            return BadRequest(new { code = "SCHEDULE_INVALID", message = exception.Message });
        }
        catch (ScheduleConflictException exception)
        {
            return Conflict(new { code = "SCHEDULE_CONFLICT", message = exception.Message });
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(new { code = "SCHEDULE_LIMIT", message = exception.Message });
        }
    }

    [HttpGet("gantt")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<GanttScheduleDto>> GetGantt(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.GetGanttAsync(from, to, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { code = "INVALID_RANGE", message = exception.Message });
        }
    }

    [HttpGet("workorders/{id:int}")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ScheduledWorkOrderDto>> GetWorkOrder(int id, CancellationToken cancellationToken)
    {
        ScheduledWorkOrderDto? result = await _service.GetWorkOrderAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("operations/{id:int}")]
    [Authorize(Policy = "RequireTeamLead")]
    public async Task<ActionResult<ScheduledOperationDto>> Adjust(
        int id,
        AdjustOperationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.AdjustOperationAsync(
                id, request.EquipmentId, request.PlannedStartUtc, request.PlannedEndUtc, request.Version, cancellationToken));
        }
        catch (ScheduleConflictException exception)
        {
            return Conflict(new { code = "SCHEDULE_CONFLICT", message = exception.Message });
        }
        catch (ScheduleValidationException exception)
        {
            return BadRequest(new { code = "SCHEDULE_INVALID", message = exception.Message });
        }
    }
}
