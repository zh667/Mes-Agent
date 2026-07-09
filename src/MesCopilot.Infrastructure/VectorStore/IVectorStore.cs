using MesCopilot.Domain.Entities.Knowledge;

namespace MesCopilot.Infrastructure.VectorStore;

public interface IVectorStore
{
    Task<float[]> GenerateEmbeddingAsync(string text);

    Task StoreChunkAsync(DocumentChunk chunk);

    Task<List<DocumentChunk>> SearchSimilarAsync(string query, int topK = 5, double similarityThreshold = 0.7);
}
