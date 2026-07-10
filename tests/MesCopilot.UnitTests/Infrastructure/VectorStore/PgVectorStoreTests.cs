using MesCopilot.Domain.Entities.Knowledge;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.UnitTests.Infrastructure.VectorStore;

public class PgVectorStoreTests
{
    [Fact]
    public async Task GenerateEmbeddingAsync_ShouldDelegateToEmbeddingClient()
    {
        await using MesDbContext context = CreateContext();
        var embeddingClient = new FakeEmbeddingClient([0.1f, 0.2f]);
        var store = new PgVectorStore(context, embeddingClient);

        float[] embedding = await store.GenerateEmbeddingAsync("alarm handling");

        Assert.Equal([0.1f, 0.2f], embedding);
        Assert.Equal("alarm handling", embeddingClient.LastText);
    }

    [Fact]
    public async Task StoreChunkAsync_ShouldPersistChunkWithVector()
    {
        await using MesDbContext context = CreateContext();
        await SeedDocumentAsync(context);
        var store = new PgVectorStore(context, new FakeEmbeddingClient([0.1f]));
        var chunk = new DocumentChunk
        {
            DocumentId = 1,
            Sequence = 1,
            Content = "Alarm handling SOP",
            Vector = "[0.1,0.2]",
            TokenCount = 4,
            CreatedAt = DateTime.UtcNow
        };

        await store.StoreChunkAsync(chunk);

        DocumentChunk stored = await context.DocumentChunks.SingleAsync();
        Assert.Equal("Alarm handling SOP", stored.Content);
        Assert.Equal("[0.1,0.2]", stored.Vector);
    }

    [Fact]
    public async Task StoreChunkAsync_EmptyVector_ShouldThrow()
    {
        await using MesDbContext context = CreateContext();
        var store = new PgVectorStore(context, new FakeEmbeddingClient([0.1f]));
        var chunk = new DocumentChunk
        {
            Content = "No vector",
            Vector = "",
            CreatedAt = DateTime.UtcNow
        };

        await Assert.ThrowsAsync<ArgumentException>(() => store.StoreChunkAsync(chunk));
    }

    [Fact]
    public async Task SearchSimilarAsync_InvalidTopK_ShouldThrow()
    {
        await using MesDbContext context = CreateContext();
        var store = new PgVectorStore(context, new FakeEmbeddingClient([0.1f]));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => store.SearchSimilarAsync("alarm", topK: 0));
    }

    private static MesDbContext CreateContext()
    {
        return Tenancy.TenantTestDbContextFactory.Create();
    }

    private static async Task SeedDocumentAsync(MesDbContext context)
    {
        context.Documents.Add(new Document
        {
            Id = 1,
            Title = "SOP A102",
            FileName = "sop-a102.pdf",
            FilePath = "/docs/sop-a102.pdf",
            Type = DocumentType.Sop,
            FileSize = 2048,
            MimeType = "application/pdf",
            UploadedAt = DateTime.UtcNow,
            VectorizationStatus = "completed"
        });

        await context.SaveChangesAsync();
    }

    private sealed class FakeEmbeddingClient : IEmbeddingClient
    {
        private readonly float[] _embedding;

        public FakeEmbeddingClient(float[] embedding)
        {
            _embedding = embedding;
        }

        public string? LastText { get; private set; }

        public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            LastText = text;
            return Task.FromResult(_embedding);
        }
    }
}
