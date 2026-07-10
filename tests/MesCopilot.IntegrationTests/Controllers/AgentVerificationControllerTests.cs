using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MesCopilot.Api.Dtos.Agent;
using MesCopilot.Domain.Entities.Conversations;
using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;
using MesCopilot.IntegrationTests.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.IntegrationTests.Controllers;

public sealed class AgentVerificationControllerTests : IClassFixture<AgentApiFactory>
{
    private readonly AgentApiFactory _factory;

    public AgentVerificationControllerTests(AgentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetVerification_WhenMessageOwnedByUser_ReturnsPersistedResult()
    {
        Guid messageId = await AddVerifiedMessageAsync(TestAuthHandler.DefaultUserId);
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/api/agent/messages/{messageId}/verification");

        response.EnsureSuccessStatusCode();
        VerificationResultDto? result = await response.Content.ReadFromJsonAsync<VerificationResultDto>();
        Assert.NotNull(result);
        Assert.Equal("Verified", result.Status);
        Assert.Equal("Delayed work order count", Assert.Single(result.Checks).Claim);
    }

    [Fact]
    public async Task GetVerification_WhenMessageOwnedByAnotherUser_ReturnsNotFound()
    {
        Guid messageId = await AddVerifiedMessageAsync("other-user");
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/api/agent/messages/{messageId}/verification");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetVerification_WhenMessageHasNoVerification_ReturnsNotFound()
    {
        Guid messageId = await AddVerifiedMessageAsync(TestAuthHandler.DefaultUserId, verificationJson: null);
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/api/agent/messages/{messageId}/verification");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Recheck_WhenMessageBelongsToAnotherTenant_ReturnsNotFound()
    {
        const string otherTenantId = "00000000-0000-0000-0000-000000000099";
        Guid messageId = await AddVerifiedMessageAsync(
            TestAuthHandler.DefaultUserId,
            tenantId: otherTenantId);
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            $"/api/agent/messages/{messageId}/verification/recheck",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> AddVerifiedMessageAsync(
        string userId,
        string? verificationJson = "default",
        string tenantId = SeedData.DefaultTenantId)
    {
        Guid conversationId = Guid.NewGuid();
        Guid messageId = Guid.NewGuid();
        using IServiceScope scope = _factory.Services.CreateScope();
        CurrentTenantContext tenant = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
        tenant.Initialize(new TenantResolution(tenantId, IsPlatformAdmin: false));
        MesDbContext context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
        if (tenantId != SeedData.DefaultTenantId &&
            !await context.Tenants.IgnoreQueryFilters().AnyAsync(item => item.Id == tenantId))
        {
            context.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Code = "OTHER",
                Name = "Other tenant"
            });
        }
        context.Conversations.Add(new Conversation
        {
            TenantId = tenantId,
            Id = conversationId,
            UserId = userId,
            Mode = AgentMode.Production,
            Title = "Verification test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Messages =
            {
                new ConversationMessage
                {
                    TenantId = tenantId,
                    Id = messageId,
                    Role = MessageRole.Assistant,
                    Content = "Two delayed orders.",
                    VerificationJson = verificationJson is null ? null : JsonSerializer.Serialize(new VerificationResultDto(
                        "Verified",
                        "Primary data matched.",
                        null,
                        DateTime.UtcNow,
                        [new VerificationCheckDto("Delayed work order count", "2", "2", "DelayedOrdersVerificationRule", "Verified")])),
                    VerificationSchemaVersion = verificationJson is null ? null : 1,
                    CreatedAt = DateTime.UtcNow
                }
            }
        });
        await context.SaveChangesAsync();
        return messageId;
    }
}
