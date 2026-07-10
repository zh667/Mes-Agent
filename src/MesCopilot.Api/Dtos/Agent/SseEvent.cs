using System.Text.Json;

namespace MesCopilot.Api.Dtos.Agent;

public record SseEvent(string Type, string? Content = null, object? Data = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string ToSseFormat()
    {
        string json = JsonSerializer.Serialize(new
        {
            type = Type,
            content = Content,
            data = Data
        }, JsonOptions);

        return $"data: {json}\n\n";
    }
}
