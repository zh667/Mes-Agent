using System.Net;
using System.Net.Http.Json;
using MesCopilot.Application.Dtos.Auditing;
using MesCopilot.Domain.Entities.Auditing;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.IntegrationTests.Controllers;

public class AuditControllerTests : IClassFixture<WorkOrdersApiFactory>
{
    private readonly WorkOrdersApiFactory _factory;

    public AuditControllerTests(WorkOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOperations_ReturnsTenantScopedDatabasePage()
    {
        await SeedOperationAsync();
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/audit/operations?skip=0&take=10");

        response.EnsureSuccessStatusCode();
        PagedAuditResultDto<AuditLogDto>? result =
            await response.Content.ReadFromJsonAsync<PagedAuditResultDto<AuditLogDto>>();
        Assert.NotNull(result);
        Assert.Contains(result.Items, item => item.RouteTemplate == "api/test-audit");
    }

    [Fact]
    public async Task GetOperations_WithTakeAboveLimit_ReturnsBadRequest()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/audit/operations?take=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task SeedOperationAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        CurrentTenantContext tenantContext = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
        tenantContext.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: false));
        MesDbContext context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
        context.AuditLogs.Add(new AuditLog
        {
            TenantId = SeedData.DefaultTenantId,
            Method = "GET",
            RouteTemplate = "api/test-audit",
            QueryKeys = string.Empty,
            StatusCode = 200,
            DurationMilliseconds = 3
        });
        await context.SaveChangesAsync();
    }
}
