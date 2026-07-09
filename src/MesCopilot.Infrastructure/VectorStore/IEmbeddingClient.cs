namespace MesCopilot.Infrastructure.VectorStore;

public interface IEmbeddingClient
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
