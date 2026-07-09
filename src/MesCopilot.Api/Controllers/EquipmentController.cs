using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MesCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EquipmentController : ControllerBase
{
    private readonly IEquipmentService _equipmentService;

    public EquipmentController(IEquipmentService equipmentService)
    {
        _equipmentService = equipmentService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EquipmentDto>>> GetAll()
    {
        return Ok(await _equipmentService.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipmentDto>> GetById(int id)
    {
        var equipment = await _equipmentService.GetByIdAsync(id);
        return equipment is null ? NotFound() : Ok(equipment);
    }

    [HttpGet("{id:int}/status-history")]
    public async Task<ActionResult<IEnumerable<EquipmentStatusDto>>> GetStatusHistory(int id)
    {
        return Ok(await _equipmentService.GetStatusHistoryAsync(id));
    }

    [HttpGet("{id:int}/alarms")]
    public async Task<ActionResult<IEnumerable<EquipmentAlarmDto>>> GetAlarms(int id)
    {
        return Ok(await _equipmentService.GetAlarmsAsync(id));
    }

    [HttpGet("{id:int}/oee")]
    public async Task<ActionResult<OeeDto>> CalculateOee(int id, [FromQuery] DateTime? date)
    {
        try
        {
            return Ok(await _equipmentService.CalculateOeeAsync(id, date ?? DateTime.UtcNow.Date));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
