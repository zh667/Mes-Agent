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

    [Fact]
    public async Task SearchSimilarAsync_ShouldSkipChunksWithMissingDocuments()
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
            VectorizationStatus = "completed"
        };
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        List<DocumentChunk> chunks =
        [
            new DocumentChunk
            {
                Id = 10,
                DocumentId = 999,
                Sequence = 1,
                Content = "Stale search result.",
                TokenCount = 3
            },
            new DocumentChunk
            {
                Id = 11,
                DocumentId = document.Id,
                Sequence = 2,
                Content = "Alarm A102 requires checking the sensor.",
                TokenCount = 7
            }
        ];
        var service = new KnowledgeService(context, [], new TextChunker(), new FakeVectorStore(chunks));

        IReadOnlyList<DocumentSearchResultDto> results = await service.SearchSimilarAsync("A102 alarm");

        DocumentSearchResultDto result = Assert.Single(results);
        Assert.Equal(11, result.ChunkId);
        Assert.Equal(document.Id, result.DocumentId);
    }

    [Fact]
    public async Task SearchSimilarAsync_TopKAboveLimit_ShouldThrow()
    {
        await using MesDbContext context = CreateContext();
        var service = new KnowledgeService(context, [], new TextChunker(), new FakeVectorStore([]));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.SearchSimilarAsync("alarm", topK: 101));
    }

    [Fact]
    public async Task UploadNewVersionAsync_CreatesNextVersionAndDeactivatesPrevious()
    {
        await using MesDbContext context = CreateContext();
        var service = new KnowledgeService(context);
        DocumentDto document = await service.CreateDocumentAsync(new CreateDocumentRequest(
            "SOP A102",
            "a102-v1.pdf",
            "/docs/a102-v1.pdf",
            DocumentType.Sop,
            512,
            "application/pdf",
            "Alarm handling"));
        context.DocumentVersions.Add(new DocumentVersion
        {
            DocumentId = document.Id,
            VersionNumber = 1,
            FileName = "a102-v1.pdf",
            FilePath = "/docs/a102-v1.pdf",
            FileSize = 512,
            UploadedById = "user-1",
            UploadedAt = DateTime.UtcNow.AddDays(-1),
            ChangeNote = "Initial upload",
            IsActive = true
        });
        await context.SaveChangesAsync();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("version 2 content"));

        DocumentVersionDto version = await service.UploadNewVersionAsync(
            document.Id,
            stream,
            "a102-v2.pdf",
            "Add reset procedure",
            "user-2");

        Assert.Equal(2, version.VersionNumber);
        Assert.Equal("a102-v2.pdf", version.FileName);
        Assert.True(version.IsActive);
        Assert.Equal(Encoding.UTF8.GetByteCount("version 2 content"), version.FileSizeBytes);

        DocumentVersion oldVersion = await context.DocumentVersions.SingleAsync(item => item.DocumentId == document.Id && item.VersionNumber == 1);
        Assert.False(oldVersion.IsActive);

        Document updatedDocument = await context.Documents.SingleAsync(item => item.Id == document.Id);
        Assert.Equal("a102-v2.pdf", updatedDocument.FileName);
        Assert.Equal("completed", updatedDocument.VectorizationStatus);
    }

    [Fact]
    public async Task UploadNewVersionAsync_PersistsVersionFileAtRecordedPath()
    {
        await using MesDbContext context = CreateContext();
        var service = new KnowledgeService(context);
        DocumentDto document = await service.CreateDocumentAsync(new CreateDocumentRequest(
            "SOP A102",
            "a102-v1.pdf",
            "/docs/a102-v1.pdf",
            DocumentType.Sop,
            512,
            "application/pdf",
            "Alarm handling"));
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("version 2 content"));

        DocumentVersionDto version = await service.UploadNewVersionAsync(
            document.Id,
            stream,
            "a102-v2.pdf",
            "Persist file",
            "user-2");

        DocumentVersion storedVersion = await context.DocumentVersions.SingleAsync(item => item.Id == version.Id);
        try
        {
            Assert.True(File.Exists(storedVersion.FilePath), $"Expected version file to exist at {storedVersion.FilePath}.");
            Assert.Equal("version 2 content", await File.ReadAllTextAsync(storedVersion.FilePath));
        }
        finally
        {
            string? directory = Path.GetDirectoryName(storedVersion.FilePath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UploadNewVersionAsync_WithParsers_ReplacesExistingChunks()
    {
        await using MesDbContext context = CreateContext();
        var parser = new FakeDocumentParser(new ParsedDocument(
            "Updated alarm reset procedure",
            [new ParsedDocumentSection(null, "Reset", "Updated alarm reset procedure")]));
        var vectorStore = new PgVectorStore(context, new FakeEmbeddingClient([0.3f, 0.4f]));
        var service = new KnowledgeService(context, [parser], new TextChunker(), vectorStore);
        DocumentDto document = await service.CreateDocumentAsync(new CreateDocumentRequest(
            "SOP A102",
            "a102-v1.pdf",
            "/docs/a102-v1.pdf",
            DocumentType.Sop,
            512,
            "application/pdf",
            "Alarm handling"));
        context.DocumentChunks.Add(new DocumentChunk
        {
            DocumentId = document.Id,
            Sequence = 1,
            Content = "Old alarm procedure",
            TokenCount = 3,
            Vector = "[0.1,0.2]",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("version 2 content"));

        await service.UploadNewVersionAsync(
            document.Id,
            stream,
            "a102-v2.pdf",
            "Replace procedure",
            "user-2");

        DocumentChunk chunk = await context.DocumentChunks.SingleAsync(item => item.DocumentId == document.Id);
        Assert.Equal("Updated alarm reset procedure", chunk.Content);
        Assert.Equal("[0.3,0.4]", chunk.Vector);
        Assert.Equal(1, chunk.Sequence);
    }

    [Fact]
    public async Task UploadNewVersionAsync_WhenVectorizationFails_CreatesVersionAndMarksDocumentFailed()
    {
        await using MesDbContext context = CreateContext();
        var parser = new FakeDocumentParser(new ParsedDocument(
            "Updated alarm reset procedure",
            [new ParsedDocumentSection(null, "Reset", "Updated alarm reset procedure")]));
        var service = new KnowledgeService(context, [parser], new TextChunker(), new FakeVectorStore([]));
        DocumentDto document = await service.CreateDocumentAsync(new CreateDocumentRequest(
            "SOP A102",
            "a102-v1.pdf",
            "/docs/a102-v1.pdf",
            DocumentType.Sop,
            512,
            "application/pdf",
            "Alarm handling"));
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("version 2 content"));

        DocumentVersionDto version = await service.UploadNewVersionAsync(
            document.Id,
            stream,
            "a102-v2.pdf",
            "Replace procedure",
            "user-2");

        Assert.Equal(1, version.VersionNumber);
        Assert.Equal("failed", (await context.Documents.SingleAsync(item => item.Id == document.Id)).VectorizationStatus);
        Assert.Empty(await context.DocumentChunks.Where(item => item.DocumentId == document.Id).ToListAsync());
    }

    [Fact]
    public async Task RevertToVersionAsync_ActivatesTargetVersionAndUpdatesDocument()
    {
        await using MesDbContext context = CreateContext();
        var service = new KnowledgeService(context);
        DocumentDto document = await service.CreateDocumentAsync(new CreateDocumentRequest(
            "SOP A102",
            "a102-v2.pdf",
            "/docs/a102-v2.pdf",
            DocumentType.Sop,
            1024,
            "application/pdf",
            "Alarm handling"));
        context.DocumentVersions.AddRange(
            new DocumentVersion
            {
                DocumentId = document.Id,
                VersionNumber = 1,
                FileName = "a102-v1.pdf",
                FilePath = "/docs/a102-v1.pdf",
                FileSize = 512,
                UploadedById = "user-1",
                UploadedAt = DateTime.UtcNow.AddDays(-2),
                ChangeNote = "Initial upload",
                IsActive = false
            },
            new DocumentVersion
            {
                DocumentId = document.Id,
                VersionNumber = 2,
                FileName = "a102-v2.pdf",
                FilePath = "/docs/a102-v2.pdf",
                FileSize = 1024,
                UploadedById = "user-2",
                UploadedAt = DateTime.UtcNow.AddDays(-1),
                ChangeNote = "Second upload",
                IsActive = true
            });
        context.DocumentChunks.Add(new DocumentChunk
        {
            DocumentId = document.Id,
            Sequence = 1,
            Content = "Current version chunk",
            TokenCount = 3,
            Vector = "[0.1,0.2]",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        int targetVersionId = await context.DocumentVersions
            .Where(item => item.DocumentId == document.Id && item.VersionNumber == 1)
            .Select(item => item.Id)
            .SingleAsync();

        await service.RevertToVersionAsync(document.Id, targetVersionId);

        List<DocumentVersion> versions = await context.DocumentVersions
            .Where(item => item.DocumentId == document.Id)
            .OrderBy(item => item.VersionNumber)
            .ToListAsync();
        Assert.True(versions[0].IsActive);
        Assert.False(versions[1].IsActive);

        Document updatedDocument = await context.Documents.SingleAsync(item => item.Id == document.Id);
        Assert.Equal("a102-v1.pdf", updatedDocument.FileName);
        Assert.Equal("/docs/a102-v1.pdf", updatedDocument.FilePath);
        Assert.Equal("pending", updatedDocument.VectorizationStatus);
        Assert.Empty(await context.DocumentChunks.Where(item => item.DocumentId == document.Id).ToListAsync());
    }

    private static MesDbContext CreateContext()
    {
        return Infrastructure.Tenancy.TenantTestDbContextFactory.Create();
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
