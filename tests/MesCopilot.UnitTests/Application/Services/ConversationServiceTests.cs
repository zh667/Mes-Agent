using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.UnitTests.Application.Services;

public sealed class ConversationServiceTests : IDisposable
{
    private readonly MesDbContext _context;
    private readonly ConversationService _service;

    public ConversationServiceTests()
    {
        _context = Infrastructure.Tenancy.TenantTestDbContextFactory.Create();
        _service = new ConversationService(_context);
    }

    [Fact]
    public async Task CreateConversationAsync_CreatesUserScopedConversationWithInitialMessage()
    {
        var result = await _service.CreateConversationAsync(
            "user-1",
            AgentMode.Production,
            "Which work orders are delayed today?");

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(AgentMode.Production, result.Mode);
        Assert.Equal(1, result.MessageCount);

        var detail = await _service.GetConversationAsync(result.Id, "user-1");
        Assert.NotNull(detail);
        Assert.Equal("Which work orders are delayed today?", detail.Messages.Single().Content);
        Assert.Equal(MessageRole.User, detail.Messages.Single().Role);
    }

    [Fact]
    public async Task CreateConversationAsync_TruncatesLongTitle()
    {
        string message = new('a', 80);

        var result = await _service.CreateConversationAsync("user-1", AgentMode.Knowledge, message);

        Assert.True(result.Title.Length <= 30);
        Assert.EndsWith("...", result.Title);
    }

    [Fact]
    public async Task GetConversationAsync_ReturnsNullForWrongUser()
    {
        var created = await _service.CreateConversationAsync("user-1", AgentMode.Oee, "Line 2 OEE?");

        var result = await _service.GetConversationAsync(created.Id, "user-2");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetContextWindowAsync_ReturnsLatestMessagesInChronologicalOrder()
    {
        var created = await _service.CreateConversationAsync("user-1", AgentMode.Production, "message-01");
        for (int i = 2; i <= 15; i++)
        {
            await _service.AddMessageAsync(created.Id, MessageRole.Assistant, $"message-{i:00}");
        }

        var window = await _service.GetContextWindowAsync(created.Id, maxMessages: 5);

        Assert.Equal(5, window.Count);
        Assert.Equal("message-11", window[0].Content);
        Assert.Equal("message-15", window[^1].Content);
    }

    [Fact]
    public async Task DeleteConversationAsync_OnlyDeletesOwnerConversation()
    {
        var created = await _service.CreateConversationAsync("user-1", AgentMode.Quality, "Trace a batch");

        await _service.DeleteConversationAsync(created.Id, "other-user");

        Assert.NotNull(await _service.GetConversationAsync(created.Id, "user-1"));

        await _service.DeleteConversationAsync(created.Id, "user-1");

        Assert.Null(await _service.GetConversationAsync(created.Id, "user-1"));
    }

    [Fact]
    public async Task SearchConversationsAsync_ReturnsUserScopedMatchesWithSnippet()
    {
        var target = await _service.CreateConversationAsync("user-1", AgentMode.Quality, "Trace batch B-001");
        await _service.AddMessageAsync(target.Id, MessageRole.Assistant, "Batch B-001 has one defect record from final inspection.");
        var otherUser = await _service.CreateConversationAsync("user-2", AgentMode.Quality, "Trace batch B-002");
        await _service.AddMessageAsync(otherUser.Id, MessageRole.Assistant, "Batch B-002 has one defect record from final inspection.");

        var results = await _service.SearchConversationsAsync("defect", "user-1");

        var result = Assert.Single(results);
        Assert.Equal(target.Id, result.ConversationId);
        Assert.Equal("Trace batch B-001", result.Title);
        Assert.Contains("defect", result.MatchedSnippet, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, result.MessageCount);
    }

    [Fact]
    public async Task SearchConversationsAsync_BlankQuery_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SearchConversationsAsync(" ", "user-1"));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
