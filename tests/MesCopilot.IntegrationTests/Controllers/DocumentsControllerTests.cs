using System.Net;
using System.Net.Http.Json;
using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Enums;

namespace MesCopilot.IntegrationTests.Controllers;

public class DocumentsControllerTests : IClassFixture<WorkOrdersApiFactory>
{
    private readonly HttpClient _client;

    public DocumentsControllerTests(WorkOrdersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ShouldReturnSuccess()
    {
        var response = await _client.GetAsync("/api/documents");

        response.EnsureSuccessStatusCode();
        var documents = await response.Content.ReadFromJsonAsync<List<DocumentDto>>();
        Assert.NotNull(documents);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedDocument()
    {
        var request = CreateRequest("SOP API 001", "sop-api-001.pdf");

        var response = await _client.PostAsJsonAsync("/api/documents", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.NotNull(document);
        Assert.Equal("SOP API 001", document.Title);
        Assert.Equal("pending", document.VectorizationStatus);
    }

    [Fact]
    public async Task GetById_ExistingId_ShouldReturnDocument()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/documents", CreateRequest("SOP API 002", "sop-api-002.pdf"));
        var created = await createResponse.Content.ReadFromJsonAsync<DocumentDto>();

        var response = await _client.GetAsync($"/api/documents/{created!.Id}");

        response.EnsureSuccessStatusCode();
        var document = await response.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.NotNull(document);
        Assert.Equal(created.Id, document.Id);
    }

    [Fact]
    public async Task Delete_ExistingId_ShouldDeleteDocument()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/documents", CreateRequest("SOP API 003", "sop-api-003.pdf"));
        var created = await createResponse.Content.ReadFromJsonAsync<DocumentDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/documents/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var getResponse = await _client.GetAsync($"/api/documents/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private static CreateDocumentRequest CreateRequest(string title, string fileName)
    {
        return new CreateDocumentRequest(title, fileName, $"/docs/{fileName}", DocumentType.Sop, 2048, "application/pdf", "API test");
    }
}
