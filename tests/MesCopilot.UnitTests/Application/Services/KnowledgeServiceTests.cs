using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Entities.Knowledge;
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

    [Fact]
    public async Task UploadDocumentAsync_ShouldLimitEmbeddingConcurrency()
    {
        await using var context = CreateContext();
        string longText = string.Join(" ", Enumerable.Range(1, 6000).Select(index => $"word{index:0000}"));
        var parser = new FakeDocumentParser(new ParsedDocument(
            longText,
            [new ParsedDocumentSection(null, "Long SOP", longText)]));
        var vectorStore = new ConcurrencyTrackingVectorStore();
        var service = new KnowledgeService(context, [parser], new TextChunker(), vectorStore);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("ignored"));

        await service.UploadDocumentAsync(new UploadDocumentRequest(
            "Long SOP",
            "long-sop.docx",
            "/docs/long-sop.docx",
            DocumentType.Sop,
            1024,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "Long document",
            content));

        Assert.True(await context.DocumentChunks.CountAsync() > 10);
        Assert.InRange(vectorStore.MaxConcurrentCalls, 1, 10);
    }

    [Fact]
    public async Task SearchSimilarAsync_ShouldReturnChunksWithDocumentSources()
    {
        await using MesDbContext context = CreateContext();
        var document = new Document
        {
            Title = "SOP A102 Alarm Handling",
            FileName = "sop-a102.pdf",
            FilePath = "/docs/sop-a102.pdf",
            Type = DocumentType.Sop,
            FileSize = 2048,
            MimeType = "application/pdf",
            UploadedAt = DateTime.UtcNow,
            VectorizationStatus = "completed",
            Description = "Alarm handling"
        };
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var chunk = new DocumentChunk
        {
            Id = 10,
            DocumentId = document.Id,
            Sequence = 2,
            Content = "Alarm A102 requires stopping the line and checking the sensor.",
            TokenCount = 11,
            PageNumber = 4,
            SectionTitle = "A102 alarm"
        };
        var vectorStore = new FakeVectorStore([chunk]);
        var service = new KnowledgeService(context, [], new TextChunker(), vectorStore);

        IReadOnlyList<DocumentSearchResultDto> results = await service.SearchSimilarAsync(
            "A102 alarm",
            topK: 3,
            similarityThreshold: 0.6);

        DocumentSearchResultDto result = Assert.Single(results);
        Assert.Equal(10, result.ChunkId);
        Assert.Equal(document.Id, result.DocumentId);
        Assert.Equal("SOP A102 Alarm Handling", result.DocumentTitle);
        Assert.Equal("sop-a102.pdf", result.FileName);
        Assert.Equal(DocumentType.Sop, result.DocumentType);
        Assert.Equal("Alarm A102 requires stopping the line and checking the sensor.", result.Content);
        Assert.Equal(2, result.Sequence);
        Assert.Equal(4, result.PageNumber);
        Assert.Equal("A102 alarm", result.SectionTitle);
        Assert.Equal("A102 alarm", vectorStore.LastQuery);
        Assert.Equal(3, vectorStore.LastTopK);
        Assert.Equal(0.6, vectorStore.LastSimilarityThreshold);
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

    private sealed class ConcurrencyTrackingVectorStore : IVectorStore
    {
        private int _currentCalls;

        public int MaxConcurrentCalls { get; private set; }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            int currentCalls = Interlocked.Increment(ref _currentCalls);
            MaxConcurrentCalls = Math.Max(MaxConcurrentCalls, currentCalls);
            try
            {
                await Task.Delay(10);
                return [0.1f, 0.2f];
            }
            finally
            {
                Interlocked.Decrement(ref _currentCalls);
            }
        }

        public Task StoreChunkAsync(DocumentChunk chunk)
        {
            throw new NotSupportedException();
        }

        public Task<List<DocumentChunk>> SearchSimilarAsync(string query, int topK = 5, double similarityThreshold = 0.7)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeVectorStore : IVectorStore
    {
        private readonly List<DocumentChunk> _chunks;

        public FakeVectorStore(List<DocumentChunk> chunks)
        {
            _chunks = chunks;
        }

        public string? LastQuery { get; private set; }

        public int? LastTopK { get; private set; }

        public double? LastSimilarityThreshold { get; private set; }

        public Task<float[]> GenerateEmbeddingAsync(string text)
        {
            throw new NotSupportedException();
        }

        public Task StoreChunkAsync(DocumentChunk chunk)
        {
            throw new NotSupportedException();
        }

        public Task<List<DocumentChunk>> SearchSimilarAsync(string query, int topK = 5, double similarityThreshold = 0.7)
        {
            LastQuery = query;
            LastTopK = topK;
            LastSimilarityThreshold = similarityThreshold;
            return Task.FromResult(_chunks);
        }
    }
}
