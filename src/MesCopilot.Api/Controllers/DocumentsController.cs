using System.Security.Claims;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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

    [HttpGet("{id:int}/versions")]
    public async Task<ActionResult<IEnumerable<DocumentVersionDto>>> GetVersionHistory(int id)
    {
        var document = await _knowledgeService.GetByIdAsync(id);
        if (document is null)
        {
            return NotFound();
        }

        return Ok(await _knowledgeService.GetVersionHistoryAsync(id));
    }

    [HttpPost("{id:int}/versions")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<DocumentVersionDto>> UploadNewVersion(
        int id,
        IFormFile file,
        [FromForm] string changeNote)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { error = "Version file is required." });
        }

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        try
        {
            await using Stream stream = file.OpenReadStream();
            DocumentVersionDto version = await _knowledgeService.UploadNewVersionAsync(
                id,
                stream,
                file.FileName,
                changeNote ?? string.Empty,
                userId);
            return Ok(version);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:int}/versions/{versionId:int}/revert")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> RevertToVersion(int id, int versionId)
    {
        try
        {
            await _knowledgeService.RevertToVersionAsync(id, versionId);
            return NoContent();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _knowledgeService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
