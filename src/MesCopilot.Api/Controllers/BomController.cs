using MesCopilot.Api.Dtos.Bom;
using MesCopilot.Application.Dtos.Bom;
using MesCopilot.Application.Services.Bom;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesCopilot.Api.Controllers;

[ApiController]
[Route("api/bom")]
[Authorize(Policy = "RequireOperator")]
public sealed class BomController : ControllerBase
{
    private readonly IBomExplosionService _service;

    public BomController(IBomExplosionService service)
    {
        _service = service;
    }

    [HttpPost("explode")]
    [ProducesResponseType(typeof(BomExplosionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BomErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BomErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BomErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BomExplosionResultDto>> Explode(
        BomExplosionRequest request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(request.ProductId, request.Quantity, cancellationToken);
    }

    [HttpGet("products")]
    public async Task<ActionResult<IReadOnlyList<BomProductOptionDto>>> GetProducts(
        CancellationToken cancellationToken)
    {
        return Ok(await _service.GetAvailableProductsAsync(cancellationToken));
    }

    [HttpGet("{productId:int}/tree")]
    [ProducesResponseType(typeof(BomExplosionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BomErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BomErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BomExplosionResultDto>> GetTree(
        int productId,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(productId, 1m, cancellationToken);
    }

    private async Task<ActionResult<BomExplosionResultDto>> ExecuteAsync(
        int productId,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.ExplodeAsync(productId, quantity, cancellationToken));
        }
        catch (BomNotFoundException)
        {
            return NotFound(new BomErrorResponse("BOM_NOT_FOUND"));
        }
        catch (BomCycleException exception)
        {
            return Conflict(new BomErrorResponse("BOM_CYCLE", exception.Message));
        }
        catch (BomDepthExceededException exception)
        {
            return Conflict(new BomErrorResponse("BOM_DEPTH_EXCEEDED", exception.Message));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(new BomErrorResponse("INVALID_QUANTITY", exception.Message));
        }
    }
}
