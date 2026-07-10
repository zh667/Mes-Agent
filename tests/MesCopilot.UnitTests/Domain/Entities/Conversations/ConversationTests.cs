using MesCopilot.Domain.Entities.Conversations;
using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Domain.Entities.Conversations;

public class ConversationTests
{
    [Fact]
    public void Conversation_DefaultMessages_IsEmptyCollection()
    {
        Conversation conversation = new();

        Assert.NotNull(conversation.Messages);
        Assert.Empty(conversation.Messages);
    }

    [Fact]
    public void Conversation_CanStoreUserOwnerAndAgentMode()
    {
        Guid conversationId = Guid.NewGuid();
        DateTime createdAt = DateTime.UtcNow;
        Conversation conversation = new()
        {
            Id = conversationId,
            UserId = "user-123",
            Mode = AgentMode.Production,
            Title = "Delayed work orders",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        Assert.Equal(conversationId, conversation.Id);
        Assert.Equal("user-123", conversation.UserId);
        Assert.Equal(AgentMode.Production, conversation.Mode);
        Assert.Equal("Delayed work orders", conversation.Title);
    }

    [Fact]
    public void ConversationMessage_CanStoreJsonToolResults()
    {
        Guid conversationId = Guid.NewGuid();
        ConversationMessage message = new()
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = MessageRole.Tool,
            Content = "Tool returned data",
            ToolResults = """{"totalCount":2,"tool":"GetTodayWorkOrders"}""",
            CreatedAt = DateTime.UtcNow
        };

        Assert.Equal(conversationId, message.ConversationId);
        Assert.Equal(MessageRole.Tool, message.Role);
        Assert.Contains("GetTodayWorkOrders", message.ToolResults);
    }

    [Fact]
    public void MessageRole_DefinesExpectedConversationRoles()
    {
        MessageRole[] roles = Enum.GetValues<MessageRole>();

        Assert.Contains(MessageRole.User, roles);
        Assert.Contains(MessageRole.Assistant, roles);
        Assert.Contains(MessageRole.Tool, roles);
        Assert.Contains(MessageRole.System, roles);
    }
}
