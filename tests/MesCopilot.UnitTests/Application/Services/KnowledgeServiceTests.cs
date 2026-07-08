using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.UnitTests.Application.Services;

public class KnowledgeServiceTests
{
    [Fact]
    public async Task CreateDocumentAsync_ShouldPersistDocumentMetadata()
    {
        await using var context = CreateContext();
        var service = new KnowledgeService(context);

        var result = await service.CreateDocumentAsync(new CreateDocumentRequest("SOP A102", "a102.pdf", "/docs/a102.pdf", DocumentType.Sop, 1024, "application/pdf", "Alarm handling"));

        Assert.Equal("SOP A102", result.Title);
        Assert.Equal("pending", result.VectorizationStatus);
        Assert.Equal(1, await context.Documents.CountAsync());
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnDocuments()
    {
        await using var context = CreateContext();
        var service = new KnowledgeService(context);
        await service.CreateDocumentAsync(new CreateDocumentRequest("SOP A102", "a102.pdf", "/docs/a102.pdf", DocumentType.Sop, 1024, "application/pdf", null));

        var result = (await service.GetAllAsync()).ToList();

        Assert.Single(result);
        Assert.Equal("a102.pdf", result[0].FileName);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveDocument_WhenExists()
    {
        await using var context = CreateContext();
        var service = new KnowledgeService(context);
        var document = await service.CreateDocumentAsync(new CreateDocumentRequest("SOP A102", "a102.pdf", "/docs/a102.pdf", DocumentType.Sop, 1024, "application/pdf", null));

        var deleted = await service.DeleteAsync(document.Id);

        Assert.True(deleted);
        Assert.Empty(await context.Documents.ToListAsync());
    }

    private static MesDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MesDbContext(options);
    }
}
