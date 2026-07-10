using System.Text.Json;
using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Entities.Conversations;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Application.Services;

public class ConversationService : IConversationService
{
    private const int MaxTitleLength = 30;

    private readonly MesDbContext _context;

    public ConversationService(MesDbContext context)
    {
        _context = context;
    }

    public async Task<ConversationDto> CreateConversationAsync(string userId, AgentMode mode, string initialMessage)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        string normalizedMessage = NormalizeMessage(initialMessage);
        DateTime now = DateTime.UtcNow;
        Conversation conversation = new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Mode = mode,
            Title = GenerateTitle(normalizedMessage),
            CreatedAt = now,
            UpdatedAt = now
        };

        conversation.Messages.Add(new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = normalizedMessage,
            CreatedAt = now
        });

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        return ToDto(conversation, conversation.Messages.Count);
    }

    public async Task<ConversationDetailDto?> GetConversationAsync(Guid conversationId, string userId)
    {
        Conversation? conversation = await _context.Conversations
            .AsNoTracking()
            .Include(conversation => conversation.Messages.OrderBy(message => message.CreatedAt))
            .FirstOrDefaultAsync(conversation =>
                conversation.Id == conversationId &&
                conversation.UserId == userId);

        return conversation is null ? null : ToDetailDto(conversation);
    }

    public async Task<IReadOnlyList<ConversationDto>> GetUserConversationsAsync(string userId, int page = 1, int pageSize = 20)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        int cappedPageSize = Math.Min(pageSize, 100);

        return await _context.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.UserId == userId)
            .OrderByDescending(conversation => conversation.UpdatedAt)
            .Skip((page - 1) * cappedPageSize)
            .Take(cappedPageSize)
            .Select(conversation => new ConversationDto(
                conversation.Id,
                conversation.Mode,
                conversation.Title,
                conversation.CreatedAt,
                conversation.UpdatedAt,
                conversation.Messages.Count))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ConversationMessageDto>> GetContextWindowAsync(Guid conversationId, int maxMessages = 10)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxMessages);

        List<ConversationMessage> messages = await _context.ConversationMessages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.CreatedAt)
            .Take(maxMessages)
            .ToListAsync();

        return messages
            .OrderBy(message => message.CreatedAt)
            .Select(ToMessageDto)
            .ToList();
    }

    public async Task<Guid> AddMessageAsync(
        Guid conversationId,
        MessageRole role,
        string content,
        string? toolResults = null,
        string? verificationJson = null,
        int? verificationSchemaVersion = null)
    {
        string normalizedContent = NormalizeMessage(content);
        DateTime now = DateTime.UtcNow;
        Conversation? conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation is null)
        {
            throw new InvalidOperationException($"Conversation {conversationId} not found.");
        }

        Guid messageId = Guid.NewGuid();
        _context.ConversationMessages.Add(new ConversationMessage
        {
            Id = messageId,
            ConversationId = conversationId,
            Role = role,
            Content = normalizedContent,
            ToolResults = toolResults,
            VerificationJson = verificationJson,
            VerificationSchemaVersion = verificationSchemaVersion,
            CreatedAt = now
        });

        conversation.UpdatedAt = now;
        await _context.SaveChangesAsync();
        return messageId;
    }

    public async Task<ConversationMessageVerificationSourceDto?> GetMessageVerificationSourceAsync(
        Guid messageId,
        string userId)
    {
        return await _context.ConversationMessages
            .AsNoTracking()
            .Where(message => message.Id == messageId && message.Conversation.UserId == userId)
            .Select(message => new ConversationMessageVerificationSourceDto(
                message.Id,
                message.Conversation.Mode,
                message.Content,
                message.ToolResults,
                message.VerificationJson))
            .FirstOrDefaultAsync();
    }

    public async Task SaveMessageVerificationAsync(Guid messageId, string verificationJson, int schemaVersion)
    {
        ConversationMessage? message = await _context.ConversationMessages.FirstOrDefaultAsync(item => item.Id == messageId);
        if (message is null)
        {
            throw new KeyNotFoundException("Conversation message was not found.");
        }

        message.VerificationJson = verificationJson;
        message.VerificationSchemaVersion = schemaVersion;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteConversationAsync(Guid conversationId, string userId)
    {
        Conversation? conversation = await _context.Conversations
            .FirstOrDefaultAsync(conversation =>
                conversation.Id == conversationId &&
                conversation.UserId == userId);

        if (conversation is null)
        {
            return;
        }

        _context.Conversations.Remove(conversation);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ConversationSearchResultDto>> SearchConversationsAsync(
        string query,
        string userId,
        int skip = 0,
        int take = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Search query is required.", nameof(query));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        int cappedTake = Math.Min(take, 100);
        string normalizedQuery = query.Trim().ToLower();
        var matchedMessages = await _context.ConversationMessages
            .AsNoTracking()
            .Where(message => message.Content.ToLower().Contains(normalizedQuery))
            .Join(
                _context.Conversations.AsNoTracking().Where(conversation => conversation.UserId == userId),
                message => message.ConversationId,
                conversation => conversation.Id,
                (message, conversation) => new
                {
                    Message = message,
                    Conversation = conversation
                })
            .ToListAsync();

        return matchedMessages
            .GroupBy(item => item.Conversation.Id)
            .Select(group =>
            {
                var latestMatch = group.OrderByDescending(item => item.Message.CreatedAt).First();
                return new ConversationSearchResultDto(
                    latestMatch.Conversation.Id,
                    latestMatch.Conversation.Title,
                    CreateSnippet(latestMatch.Message.Content),
                    group.Max(item => item.Message.CreatedAt),
                    group.Count());
            })
            .OrderByDescending(result => result.LastMessageAt)
            .Skip(skip)
            .Take(cappedTake)
            .ToList();
    }

    private static string NormalizeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message is required.", nameof(message));
        }

        return message.Trim();
    }

    private static string GenerateTitle(string message)
    {
        return message.Length <= MaxTitleLength
            ? message
            : string.Concat(message.AsSpan(0, MaxTitleLength - 3), "...");
    }

    private static string CreateSnippet(string content)
    {
        const int maxSnippetLength = 200;
        return content.Length <= maxSnippetLength
            ? content
            : string.Concat(content.AsSpan(0, maxSnippetLength), "...");
    }

    private static ConversationDto ToDto(Conversation conversation, int messageCount)
    {
        return new ConversationDto(
            conversation.Id,
            conversation.Mode,
            conversation.Title,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            messageCount);
    }

    private static ConversationDetailDto ToDetailDto(Conversation conversation)
    {
        return new ConversationDetailDto(
            conversation.Id,
            conversation.Mode,
            conversation.Title,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            conversation.Messages.OrderBy(message => message.CreatedAt).Select(ToMessageDto).ToList());
    }

    private static ConversationMessageDto ToMessageDto(ConversationMessage message)
    {
        return new ConversationMessageDto(
            message.Id,
            message.Role,
            message.Content,
            ParseToolResults(message.ToolResults),
            ParseToolResults(message.VerificationJson),
            message.CreatedAt);
    }

    private static object? ParseToolResults(string? toolResults)
    {
        return string.IsNullOrWhiteSpace(toolResults)
            ? null
            : JsonSerializer.Deserialize<object>(toolResults);
    }
}
