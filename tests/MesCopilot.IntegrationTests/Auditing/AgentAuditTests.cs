using MesCopilot.Domain.Entities.Auditing;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;
using MesCopilot.IntegrationTests.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.IntegrationTests.Auditing;

public class AgentAuditTests : IClassFixture<AgentApiFactory>
{
    private readonly AgentApiFactory _factory;

    public AgentAuditTests(AgentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SuccessfulChat_RecordsToolAndVerificationAudit()
    {
        using HttpClient client = _factory.CreateClient();
        using StringContent content = new(
            """{"mode":0,"message":"Which work orders are delayed today?"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        HttpResponseMessage response = await client.PostAsync("/api/agent/chat", content);

        response.EnsureSuccessStatusCode();
        using IServiceScope scope = _factory.Services.CreateScope();
        CurrentTenantContext tenantContext = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
        tenantContext.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: false));
        MesDbContext context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
        AgentAuditLog audit = await context.AgentAuditLogs.OrderByDescending(item => item.Timestamp).FirstAsync();
        Assert.Equal("Completed", audit.Status);
        Assert.False(string.IsNullOrWhiteSpace(audit.ToolName));
        Assert.NotNull(audit.IsVerified);
        Assert.Equal(64, audit.QueryDigest?.Length);
    }
}
