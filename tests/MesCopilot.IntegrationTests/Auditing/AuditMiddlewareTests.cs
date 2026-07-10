using System.Net;
using System.Net.Http.Json;
using MesCopilot.Api.Dtos.Auth;
using MesCopilot.Domain.Entities.Auditing;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;
using MesCopilot.IntegrationTests.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.IntegrationTests.Auditing;

public class AuditMiddlewareTests
{
    [Fact]
    public async Task Register_RecordsHttpAuditWithoutPersistingAuthenticationBody()
    {
        using AuthApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = $"audit-{Guid.NewGuid():N}@example.com",
            Password = "Test123!",
            DisplayName = "Audit User"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using IServiceScope scope = factory.Services.CreateScope();
        CurrentTenantContext tenantContext = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
        tenantContext.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: true));
        MesDbContext context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
        AuditLog audit = await context.AuditLogs.SingleAsync(item => item.Method == "POST");
        Assert.Equal("api/Auth/register", audit.RouteTemplate, ignoreCase: true);
        Assert.Equal((int)HttpStatusCode.Created, audit.StatusCode);
        Assert.Null(audit.RequestBody);
        Assert.True(audit.DurationMilliseconds >= 0);
    }
}
