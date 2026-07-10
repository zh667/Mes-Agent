using System.Net;

namespace MesCopilot.IntegrationTests.Controllers;

public class ReportsControllerTests : IClassFixture<WorkOrdersApiFactory>
{
    private readonly HttpClient _client;

    public ReportsControllerTests(WorkOrdersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ExportProductionReport_Excel_ReturnsDownloadableXlsx()
    {
        string date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        HttpResponseMessage response = await _client.GetAsync($"/api/reports/production?date={date}&format=excel");
        byte[] bytes = await response.Content.ReadAsByteArrayAsync();

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("PK", System.Text.Encoding.ASCII.GetString(bytes, 0, 2));
    }

    [Fact]
    public async Task ExportOeeReport_Pdf_ReturnsDownloadablePdf()
    {
        string date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        HttpResponseMessage response = await _client.GetAsync($"/api/reports/oee?equipmentIds=1&from={date}&to={date}&format=pdf");
        byte[] bytes = await response.Content.ReadAsByteArrayAsync();

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task ExportProductionReport_InvalidFormat_ReturnsBadRequest()
    {
        string date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        HttpResponseMessage response = await _client.GetAsync($"/api/reports/production?date={date}&format=csv");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ExportOeeReport_DateRangeAboveLimit_ReturnsBadRequest()
    {
        DateTime from = DateTime.UtcNow.Date;
        DateTime to = from.AddDays(90);

        HttpResponseMessage response = await _client.GetAsync($"/api/reports/oee?equipmentIds=1&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&format=excel");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
