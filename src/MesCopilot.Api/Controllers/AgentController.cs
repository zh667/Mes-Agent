using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using MesCopilot.Agent.Models;
using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;
using MesCopilot.Agent.Plugins.OeeAgentPlugin;
using MesCopilot.Agent.Plugins.ProductionAgentPlugin;
using MesCopilot.Agent.Plugins.QualityAgentPlugin;
using MesCopilot.Agent.Verification;
using MesCopilot.Application.Dtos;
using MesCopilot.Api.Dtos.Agent;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesCopilot.Api.Controllers;

[ApiController]
[Route("api/agent")]
[Authorize]
public class AgentController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConversationService _conversationService;
    private readonly IFactVerifier _factVerifier;
    private readonly ProductionAgentPlugin _productionAgent;
    private readonly QualityAgentPlugin _qualityAgent;
    private readonly OeeAgentPlugin _oeeAgent;
    private readonly KnowledgeAgentPlugin _knowledgeAgent;

    public AgentController(
        IConversationService conversationService,
        IFactVerifier factVerifier,
        ProductionAgentPlugin productionAgent,
        QualityAgentPlugin qualityAgent,
        OeeAgentPlugin oeeAgent,
        KnowledgeAgentPlugin knowledgeAgent)
    {
        _conversationService = conversationService;
        _factVerifier = factVerifier;
        _productionAgent = productionAgent;
        _qualityAgent = qualityAgent;
        _oeeAgent = oeeAgent;
        _knowledgeAgent = knowledgeAgent;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat(ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message is required." });
        }

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Guid? conversationId = await PrepareConversationAsync(request, userId);
        if (!conversationId.HasValue)
        {
            return NotFound();
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            await WriteEventAsync(new SseEvent("thinking", "Selecting MES tool"), cancellationToken);

            AgentToolExecution execution = await ExecuteToolAsync(request, cancellationToken);
            VerificationResult verification = _factVerifier.Verify(execution.Result);

            await WriteEventAsync(new SseEvent("tool_result", Data: new
            {
                tool = execution.ToolName,
                data = execution.Result.Data,
                verified = verification.IsVerified,
                discrepancies = verification.Discrepancies
            }), cancellationToken);

            foreach (string token in ChunkText(execution.Result.Explanation, 24))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await WriteEventAsync(new SseEvent("token", token), cancellationToken);
            }

            await _conversationService.AddMessageAsync(
                conversationId.Value,
                MessageRole.Assistant,
                execution.Result.Explanation,
                JsonSerializer.Serialize(execution.Result.Data, JsonOptions));

            await WriteEventAsync(new SseEvent("done"), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            await WriteEventAsync(new SseEvent("error", "Agent processing failed."), cancellationToken);
        }

        return new EmptyResult();
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var conversations = await _conversationService.GetUserConversationsAsync(userId, page, pageSize);
        return Ok(conversations);
    }

    [HttpGet("conversations/search")]
    public async Task<ActionResult<IReadOnlyList<ConversationSearchResultDto>>> SearchConversations(
        [FromQuery] string q,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "Search query is required." });
        }

        var results = await _conversationService.SearchConversationsAsync(q, userId, skip, take);
        return Ok(results);
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> GetConversation(Guid id)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var conversation = await _conversationService.GetConversationAsync(id, userId);
        return conversation is null ? NotFound() : Ok(conversation);
    }

    [HttpDelete("conversations/{id:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid id)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var conversation = await _conversationService.GetConversationAsync(id, userId);
        if (conversation is null)
        {
            return NotFound();
        }

        await _conversationService.DeleteConversationAsync(id, userId);
        return NoContent();
    }

    private async Task<Guid?> PrepareConversationAsync(ChatRequest request, string userId)
    {
        if (!request.ConversationId.HasValue)
        {
            var conversation = await _conversationService.CreateConversationAsync(
                userId,
                request.Mode,
                request.Message);
            return conversation.Id;
        }

        var existing = await _conversationService.GetConversationAsync(request.ConversationId.Value, userId);
        if (existing is null)
        {
            return null;
        }

        await _conversationService.AddMessageAsync(existing.Id, MessageRole.User, request.Message);
        return existing.Id;
    }

    private async Task<AgentToolExecution> ExecuteToolAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string message = request.Message;

        return request.Mode switch
        {
            AgentMode.Production => await ExecuteProductionToolAsync(message, request.DebugMode),
            AgentMode.Quality => await ExecuteQualityToolAsync(message, request.DebugMode),
            AgentMode.Oee => await ExecuteOeeToolAsync(message, request.DebugMode),
            AgentMode.Knowledge => await ExecuteKnowledgeToolAsync(message, request.DebugMode),
            _ => await ExecuteProductionToolAsync(message, request.DebugMode)
        };
    }

    private async Task<AgentToolExecution> ExecuteProductionToolAsync(string message, bool debugMode)
    {
        if (ContainsAny(message, "schedule", "dispatch", "排程", "调度") &&
            _productionAgent.SuggestScheduleTool is not null)
        {
            FunctionCallResult scheduleResult = await _productionAgent.SuggestScheduleTool.ExecuteAsync(
                productionLineId: null,
                startDate: DateTime.UtcNow,
                endDate: DateTime.UtcNow.AddDays(7),
                debugMode: debugMode);
            return new AgentToolExecution(_productionAgent.SuggestScheduleTool.Name, scheduleResult);
        }

        FunctionCallResult result = ContainsAny(message, "delay", "delayed", "late", "延期")
            ? await _productionAgent.AnalyzeDelayedOrdersTool.ExecuteAsync(debugMode: debugMode)
            : await _productionAgent.GetTodayWorkOrdersTool.ExecuteAsync(debugMode: debugMode);

        return new AgentToolExecution(
            ContainsAny(message, "delay", "delayed", "late", "延期")
                ? _productionAgent.AnalyzeDelayedOrdersTool.Name
                : _productionAgent.GetTodayWorkOrdersTool.Name,
            result);
    }

    private async Task<AgentToolExecution> ExecuteQualityToolAsync(string message, bool debugMode)
    {
        if (ContainsAny(message, "why", "root cause", "5why", "5-why", "根因") &&
            _qualityAgent.FiveWhyAnalysisTool is not null)
        {
            FunctionCallResult whyResult = await _qualityAgent.FiveWhyAnalysisTool.ExecuteAsync(
                defectRecordId: ExtractFirstPositiveInteger(message),
                symptomDescription: message,
                debugMode);
            return new AgentToolExecution(_qualityAgent.FiveWhyAnalysisTool.Name, whyResult);
        }

        string? batchNumber = ExtractBatchNumber(message);
        if (!string.IsNullOrWhiteSpace(batchNumber))
        {
            FunctionCallResult traceResult = await _qualityAgent.TraceBatchTool.ExecuteAsync(batchNumber, debugMode);
            return new AgentToolExecution(_qualityAgent.TraceBatchTool.Name, traceResult);
        }

        FunctionCallResult result = ContainsAny(message, "process", "step", "工序")
            ? await _qualityAgent.GetDefectsByProcessTool.ExecuteAsync(ExtractFirstPositiveInteger(message) ?? 1, debugMode)
            : await _qualityAgent.AnalyzeDefectPatternTool.ExecuteAsync(debugMode);

        return new AgentToolExecution(
            ContainsAny(message, "process", "step", "工序")
                ? _qualityAgent.GetDefectsByProcessTool.Name
                : _qualityAgent.AnalyzeDefectPatternTool.Name,
            result);
    }

    private async Task<AgentToolExecution> ExecuteOeeToolAsync(string message, bool debugMode)
    {
        int equipmentId = ExtractFirstPositiveInteger(message) ?? 1;
        if (ContainsAny(message, "maintenance", "predict", "mtbf", "mttr", "维护", "预测") &&
            _oeeAgent.PredictMaintenanceTool is not null)
        {
            FunctionCallResult maintenanceResult = await _oeeAgent.PredictMaintenanceTool.ExecuteAsync(equipmentId, debugMode);
            return new AgentToolExecution(_oeeAgent.PredictMaintenanceTool.Name, maintenanceResult);
        }

        FunctionCallResult result = ContainsAny(message, "oee", "efficiency", "效率")
            ? await _oeeAgent.CalculateOeeTool.ExecuteAsync(equipmentId, DateTime.UtcNow.Date, debugMode)
            : await _oeeAgent.GetEquipmentStatusTool.ExecuteAsync(equipmentId, debugMode);

        return new AgentToolExecution(
            ContainsAny(message, "oee", "efficiency", "效率")
                ? _oeeAgent.CalculateOeeTool.Name
                : _oeeAgent.GetEquipmentStatusTool.Name,
            result);
    }

    private async Task<AgentToolExecution> ExecuteKnowledgeToolAsync(string message, bool debugMode)
    {
        Match sopMatch = Regex.Match(message, @"\b[A-Z]\d{2,}\b", RegexOptions.IgnoreCase);
        if (sopMatch.Success)
        {
            FunctionCallResult sopResult = await _knowledgeAgent.GetSopByCodeTool.ExecuteAsync(sopMatch.Value, debugMode);
            return new AgentToolExecution(_knowledgeAgent.GetSopByCodeTool.Name, sopResult);
        }

        FunctionCallResult result = await _knowledgeAgent.SearchDocumentsTool.ExecuteAsync(message, debugMode);
        return new AgentToolExecution(_knowledgeAgent.SearchDocumentsTool.Name, result);
    }

    private async Task WriteEventAsync(SseEvent sseEvent, CancellationToken cancellationToken)
    {
        await Response.WriteAsync(sseEvent.ToSseFormat(), cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private static IEnumerable<string> ChunkText(string text, int chunkSize)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield return string.Empty;
            yield break;
        }

        for (int index = 0; index < text.Length; index += chunkSize)
        {
            yield return text.Substring(index, Math.Min(chunkSize, text.Length - index));
        }
    }

    private static bool ContainsAny(string message, params string[] values)
    {
        return values.Any(value => message.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static int? ExtractFirstPositiveInteger(string message)
    {
        Match match = Regex.Match(message, @"\b\d+\b");
        if (!match.Success || !int.TryParse(match.Value, out int value) || value <= 0)
        {
            return null;
        }

        return value;
    }

    private static string? ExtractBatchNumber(string message)
    {
        Match match = Regex.Match(message, @"B\d{8,14}(?:-[A-Za-z0-9]+)?|B\d{8}-[A-Za-z0-9]+", RegexOptions.IgnoreCase);
        return match.Success ? match.Value : null;
    }

    private sealed record AgentToolExecution(string ToolName, FunctionCallResult Result);
}
