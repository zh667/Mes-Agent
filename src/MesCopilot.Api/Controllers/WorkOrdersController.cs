using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkOrdersController : ControllerBase
{
    private readonly IWorkOrderService _workOrderService;

    public WorkOrdersController(IWorkOrderService workOrderService)
    {
        _workOrderService = workOrderService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkOrderDto>>> GetAll()
    {
        var workOrders = await _workOrderService.GetAllAsync();
        return Ok(workOrders);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkOrderDto>> GetById(int id)
    {
        var workOrder = await _workOrderService.GetByIdAsync(id);
        return workOrder is null ? NotFound() : Ok(workOrder);
    }

    [HttpPost]
    public async Task<ActionResult<WorkOrderDto>> Create([FromBody] CreateWorkOrderRequest request)
    {
        var workOrder = await _workOrderService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = workOrder.Id }, workOrder);
    }

    [HttpPost("{id:int}/start")]
    public async Task<ActionResult<WorkOrderDto>> Start(int id)
    {
        return await ExecuteWorkflowAsync(() => _workOrderService.StartAsync(id));
    }

    [HttpPost("{id:int}/report")]
    public async Task<ActionResult<WorkOrderDto>> Report(int id, [FromBody] ReportProductionRequest request)
    {
        return await ExecuteWorkflowAsync(() => _workOrderService.ReportAsync(id, request));
    }

    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<WorkOrderDto>> Complete(int id)
    {
        return await ExecuteWorkflowAsync(() => _workOrderService.CompleteAsync(id));
    }

    private async Task<ActionResult<WorkOrderDto>> ExecuteWorkflowAsync(Func<Task<WorkOrderDto>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
