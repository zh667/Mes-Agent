using System.Security.Claims;
using System.Text.Json;
using MesCopilot.Agent.Models;
using MesCopilot.Agent.Verification;
using MesCopilot.Api.Dtos.Agent;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesCopilot.Api.Controllers;

[ApiController]
[Route("api/agent/messages")]
[Authorize]
public sealed class AgentVerificationController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IConversationService _conversations;
    private readonly IFactVerifier _verifier;
    private readonly ITenantContext _tenantContext;

    public AgentVerificationController(
        IConversationService conversations,
        IFactVerifier verifier,
        ITenantContext tenantContext)
    {
        _conversations = conversations;
        _verifier = verifier;
        _tenantContext = tenantContext;
    }

    [HttpGet("{messageId:guid}/verification")]
    [ProducesResponseType<VerificationResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VerificationResultDto>> Get(Guid messageId)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        ConversationMessageVerificationSourceDto? source =
            await _conversations.GetMessageVerificationSourceAsync(messageId, userId);
        if (source is null || string.IsNullOrWhiteSpace(source.VerificationJson))
        {
            return NotFound();
        }

        return DeserializeVerification(source.VerificationJson) is { } result
            ? Ok(result)
            : Ok(CreateCorruptResult());
    }

    [HttpPost("{messageId:guid}/verification/recheck")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType<VerificationResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VerificationResultDto>> Recheck(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        ConversationMessageVerificationSourceDto? source =
            await _conversations.GetMessageVerificationSourceAsync(messageId, userId);
        if (source is null || !TryParseToolResult(source.ToolResults, out string toolName, out object? data))
        {
            return NotFound();
        }

        DateTime fromUtc = DateTime.UtcNow.Date;
        VerificationResult verification = await _verifier.VerifyAsync(
            new FunctionCallResult { Data = data, Explanation = source.Content },
            new VerificationContext(
                _tenantContext.TenantId ?? throw new InvalidOperationException("Tenant context is required."),
                userId,
                source.Mode,
                toolName,
                fromUtc,
                fromUtc.AddDays(1),
                HttpContext.TraceIdentifier),
            cancellationToken);
        VerificationResultDto dto = VerificationResultDto.FromDomain(verification);
        await _conversations.SaveMessageVerificationAsync(
            messageId,
            JsonSerializer.Serialize(dto, JsonOptions),
            schemaVersion: 1);
        return Ok(dto);
    }

    private static VerificationResultDto? DeserializeVerification(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<VerificationResultDto>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryParseToolResult(string? json, out string toolName, out object? data)
    {
        toolName = string.Empty;
        data = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("tool", out JsonElement tool) ||
                tool.ValueKind != JsonValueKind.String ||
                !document.RootElement.TryGetProperty("data", out JsonElement resultData))
            {
                return false;
            }

            toolName = tool.GetString() ?? string.Empty;
            data = resultData.Clone();
            return !string.IsNullOrWhiteSpace(toolName);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static VerificationResultDto CreateCorruptResult()
    {
        return new VerificationResultDto(
            VerificationStatus.Unverified.ToString(),
            "The persisted verification result could not be read.",
            "VERIFICATION_DATA_INVALID",
            DateTime.UtcNow,
            []);
    }
}
