using System.Net;
using System.Net.Http.Json;
using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Enums;

namespace MesCopilot.IntegrationTests.Controllers;

public class QualityControllerTests : IClassFixture<WorkOrdersApiFactory>
{
    private readonly HttpClient _client;

    public QualityControllerTests(WorkOrdersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetInspections_ShouldReturnSuccess()
    {
        var response = await _client.GetAsync("/api/quality/inspections");

        response.EnsureSuccessStatusCode();
        var inspections = await response.Content.ReadFromJsonAsync<List<QualityInspectionDto>>();
        Assert.NotNull(inspections);
    }

    [Fact]
    public async Task CreateInspection_ShouldReturnCreatedInspection()
    {
        var request = new CreateQualityInspectionRequest("QI-API-001", "B-API-1", 1, 1, "qc-1", "李四", 10, 9, 1, InspectionStatus.Fail, "API test");

        var response = await _client.PostAsJsonAsync("/api/quality/inspections", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var inspection = await response.Content.ReadFromJsonAsync<QualityInspectionDto>();
        Assert.NotNull(inspection);
        Assert.Equal("QI-API-001", inspection.Code);
    }

    [Fact]
    public async Task TraceBatch_ShouldReturnBatchTrace()
    {
        var request = new CreateQualityInspectionRequest("QI-API-TRACE", "B-API-TRACE", 1, 1, "qc-1", "李四", 10, 10, 0, InspectionStatus.Pass, null);
        await _client.PostAsJsonAsync("/api/quality/inspections", request);

        var response = await _client.GetAsync("/api/quality/trace/B-API-TRACE");

        response.EnsureSuccessStatusCode();
        var trace = await response.Content.ReadFromJsonAsync<BatchTraceDto>();
        Assert.NotNull(trace);
        Assert.Equal("B-API-TRACE", trace.BatchNumber);
        Assert.NotEmpty(trace.Inspections);
    }
}
