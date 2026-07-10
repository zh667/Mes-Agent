using System.Net;
using System.Security.Claims;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Entities.Conversations;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using MesCopilot.IntegrationTests.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesCopilot.IntegrationTests.Controllers;

public class AgentControllerTests : IClassFixture<AgentApiFactory>
{
    private readonly AgentApiFactory _factory;

    public AgentControllerTests(AgentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Chat_WithoutAuthentication_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateUnauthenticatedClient();
        using StringContent content = JsonContent(new
        {
            mode = 0,
            message = "Which work orders are delayed today?"
        });

        HttpResponseMessage response = await client.PostAsync("/api/agent/chat", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Chat_WithAuthentication_ReturnsSseEvents()
    {
        HttpClient client = _factory.CreateClient();
        using StringContent content = JsonContent(new
        {
            mode = 0,
            message = "Which work orders are delayed today?"
        });

        HttpResponseMessage response = await client.PostAsync("/api/agent/chat", content);
        string body = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("data: {\"type\":\"thinking\"", body);
        Assert.Contains("data: {\"type\":\"done\"", body);
    }

    [Fact]
    public async Task Chat_WithUnknownConversation_ReturnsNotFoundBeforeStreaming()
    {
        HttpClient client = _factory.CreateClient();
        using StringContent content = JsonContent(new
        {
            conversationId = Guid.NewGuid(),
            mode = 0,
            message = "Continue the conversation"
        });

        HttpResponseMessage response = await client.PostAsync("/api/agent/chat", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    }
    [Fact]
    public async Task Conversations_AreScopedToAuthenticatedUser()
    {
        HttpClient client = _factory.CreateClient();
        using StringContent content = JsonContent(new
        {
            mode = 0,
            message = "Show today's production queue"
        });

        await client.PostAsync("/api/agent/chat", content);

        HttpResponseMessage response = await client.GetAsync("/api/agent/conversations");
        string body = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("Show today's production queue", body);
    }

    [Fact]
    public async Task DeleteConversation_WhenOwnedByAnotherUser_ReturnsNotFound()
    {
        Guid conversationId = Guid.NewGuid();
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            MesDbContext context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
            context.Conversations.Add(new Conversation
            {
                Id = conversationId,
                UserId = "other-user",
                Mode = AgentMode.Production,
                Title = "Other user's conversation",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync($"/api/agent/conversations/{conversationId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SearchConversations_ReturnsAuthenticatedUsersMatches()
    {
        Guid conversationId = Guid.NewGuid();
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            MesDbContext context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
            context.Conversations.Add(new Conversation
            {
                Id = conversationId,
                UserId = TestAuthHandler.DefaultUserId,
                Mode = AgentMode.Quality,
                Title = "Trace defect conversation",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Messages =
                {
                    new ConversationMessage
                    {
                        Id = Guid.NewGuid(),
                        Role = MessageRole.Assistant,
                        Content = "Final inspection found a defect on batch B-001.",
                        CreatedAt = DateTime.UtcNow
                    }
                }
            });
            context.Conversations.Add(new Conversation
            {
                Id = Guid.NewGuid(),
                UserId = "other-user",
                Mode = AgentMode.Quality,
                Title = "Other user defect conversation",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Messages =
                {
                    new ConversationMessage
                    {
                        Id = Guid.NewGuid(),
                        Role = MessageRole.Assistant,
                        Content = "Other user's defect message.",
                        CreatedAt = DateTime.UtcNow
                    }
                }
            });
            await context.SaveChangesAsync();
        }

        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/agent/conversations/search?q=defect");

        response.EnsureSuccessStatusCode();
        var results = await response.Content.ReadFromJsonAsync<List<ConversationSearchResultDto>>();
        ConversationSearchResultDto result = Assert.Single(results!);
        Assert.Equal(conversationId, result.ConversationId);
        Assert.Contains("defect", result.MatchedSnippet, StringComparison.OrdinalIgnoreCase);
    }

    private static StringContent JsonContent(object payload)
    {
        return new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json");
    }
}
public class AgentApiFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    public HttpClient CreateUnauthenticatedClient()
    {
        return WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "NoAuth";
                    options.DefaultChallengeScheme = "NoAuth";
                })
                    .AddScheme<AuthenticationSchemeOptions, RejectAuthHandler>("NoAuth", _ => { });
            });
        }).CreateClient();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MesDatabase"] = "Host=localhost;Port=5432;Database=mes_copilot_test;Username=postgres;Password=CHANGE_ME",
                ["Jwt:Secret"] = "ThisIsATestSecretKeyThatIsAtLeast32BytesLong!!",
                ["Jwt:Issuer"] = "MesCopilot",
                ["Jwt:Audience"] = "MesCopilotClient",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MesDbContext>>();
            services.AddDbContext<MesDbContext>(options =>
                options.UseInMemoryDatabase("mes-copilot-agent-api", _databaseRoot));
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme,
                    _ => { });

            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();
            MesDbContext context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            SeedData.SeedAsync(context).GetAwaiter().GetResult();
        });
    }
}

public class RejectAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public RejectAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        ClaimsIdentity identity = new();
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.NoResult());
    }
}
