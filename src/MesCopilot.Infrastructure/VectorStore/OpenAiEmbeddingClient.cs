using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace MesCopilot.Infrastructure.VectorStore;

public class OpenAiEmbeddingClient : IEmbeddingClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public OpenAiEmbeddingClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
        _model = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Embedding text is required.", nameof(text));
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings")
        {
            Content = JsonContent.Create(new OpenAiEmbeddingRequest(_model, text))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        OpenAiEmbeddingResponse? result = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingResponse>(cancellationToken);
        return result?.Data.FirstOrDefault()?.Embedding
            ?? throw new InvalidOperationException("OpenAI embedding response did not include an embedding.");
    }

    private record OpenAiEmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input
    );

    private record OpenAiEmbeddingResponse(
        [property: JsonPropertyName("data")] IReadOnlyList<OpenAiEmbeddingData> Data
    );

    private record OpenAiEmbeddingData(
        [property: JsonPropertyName("embedding")] float[] Embedding
    );
}
