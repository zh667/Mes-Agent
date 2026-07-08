using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.DocumentParsers;
using MesCopilot.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;
using System.Text;

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

    [Fact]
    public async Task UploadDocumentAsync_ShouldParseChunkVectorizeAndPersistChunks()
    {
        await using var context = CreateContext();
        var parser = new FakeDocumentParser(new ParsedDocument(
            "Alarm handling SOP",
            [new ParsedDocumentSection(null, "SOP", "Alarm handling SOP")]));
        var vectorStore = new PgVectorStore(context, new FakeEmbeddingClient([0.1f, 0.2f]));
        var service = new KnowledgeService(context, [parser], new TextChunker(), vectorStore);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("ignored"));

        var result = await service.UploadDocumentAsync(new UploadDocumentRequest(
            "SOP A102",
            "sop-a102.docx",
            "/docs/sop-a102.docx",
            DocumentType.Sop,
            1024,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "Alarm handling",
            content));

        Assert.Equal("completed", result.VectorizationStatus);
        Assert.Equal(1, await context.Documents.CountAsync());
        var chunk = await context.DocumentChunks.SingleAsync();
        Assert.Equal("Alarm handling SOP", chunk.Content);
        Assert.Equal("[0.1,0.2]", chunk.Vector);
    }

    private static MesDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MesDbContext(options);
    }

    private sealed class FakeDocumentParser : IDocumentParser
    {
        private readonly ParsedDocument _document;

        public FakeDocumentParser(ParsedDocument document)
        {
            _document = document;
        }

        public bool CanParse(string fileName, string mimeType)
        {
            return true;
        }

        public Task<ParsedDocument> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_document);
        }
    }

    private sealed class FakeEmbeddingClient : IEmbeddingClient
    {
        private readonly float[] _embedding;

        public FakeEmbeddingClient(float[] embedding)
        {
            _embedding = embedding;
        }

        public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_embedding);
        }
    }
}
