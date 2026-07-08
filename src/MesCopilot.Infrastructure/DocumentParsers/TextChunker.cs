namespace MesCopilot.Infrastructure.DocumentParsers;

public class TextChunker
{
    private const int ChunkSizeTokens = 800;
    private const int OverlapTokens = 100;
    private const int ApproximateCharactersPerToken = 4;

    public List<string> ChunkText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        List<string> chunks = [];
        List<string> currentChunk = [];
        int currentLength = 0;
        int maxLength = ChunkSizeTokens * ApproximateCharactersPerToken;

        foreach (string word in words)
        {
            if (currentLength + word.Length > maxLength && currentChunk.Count > 0)
            {
                chunks.Add(string.Join(" ", currentChunk));
                currentChunk = currentChunk.TakeLast(OverlapTokens / ApproximateCharactersPerToken).ToList();
                currentLength = string.Join(" ", currentChunk).Length;
            }

            currentChunk.Add(word);
            currentLength += word.Length + 1;
        }

        if (currentChunk.Count > 0)
        {
            chunks.Add(string.Join(" ", currentChunk));
        }

        return chunks;
    }
}
