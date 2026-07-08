using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MesCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IKnowledgeService _knowledgeService;

    public DocumentsController(IKnowledgeService knowledgeService)
    {
        _knowledgeService = knowledgeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetAll()
    {
        return Ok(await _knowledgeService.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DocumentDto>> GetById(int id)
    {
        var document = await _knowledgeService.GetByIdAsync(id);
        return document is null ? NotFound() : Ok(document);
    }

    [HttpPost]
    public async Task<ActionResult<DocumentDto>> Create([FromBody] CreateDocumentRequest request)
    {
        var document = await _knowledgeService.CreateDocumentAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _knowledgeService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
