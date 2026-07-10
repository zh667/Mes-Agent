using System.Net;
using System.Net.Http.Json;
using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Entities.Auditing;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;
using MesCopilot.IntegrationTests.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.IntegrationTests.Auditing;

public class DataChangeAuditTests : IClassFixture<WorkOrdersApiFactory>
{
    private readonly WorkOrdersApiFactory _factory;

    public DataChangeAuditTests(WorkOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateDocument_RecordsTenantScopedAddedChange()
    {
        using HttpClient client = _factory.CreateClient();
        CreateDocumentRequest request = new(
            "Audited SOP",
            "audited-sop.pdf",
            "/docs/audited-sop.pdf",
            DocumentType.Sop,
            128,
            "application/pdf",
            "Audit test");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/documents", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using IServiceScope scope = _factory.Services.CreateScope();
        CurrentTenantContext tenantContext = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
        tenantContext.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: false));
        MesDbContext context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
        DataChangeLog change = await context.DataChangeLogs
            .SingleAsync(item => item.EntityType == "Document" && item.ChangeType == "Added");
        Assert.Equal(SeedData.DefaultTenantId, change.TenantId);
        Assert.Contains("Audited SOP", change.NewValues, StringComparison.Ordinal);
    }
}
