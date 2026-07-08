using MesCopilot.Infrastructure.DocumentParsers;

namespace MesCopilot.UnitTests.Infrastructure.DocumentParsers;

public class TextChunkerTests
{
    [Fact]
    public void ChunkText_EmptyText_ShouldReturnEmptyList()
    {
        var chunker = new TextChunker();

        List<string> chunks = chunker.ChunkText("");

        Assert.Empty(chunks);
    }

    [Fact]
    public void ChunkText_ShortText_ShouldReturnSingleChunk()
    {
        var chunker = new TextChunker();

        List<string> chunks = chunker.ChunkText("one two three");

        string chunk = Assert.Single(chunks);
        Assert.Equal("one two three", chunk);
    }

    [Fact]
    public void ChunkText_LongText_ShouldCreateMultipleChunksWithOverlap()
    {
        var chunker = new TextChunker();
        string text = string.Join(" ", Enumerable.Range(1, 900).Select(index => $"word{index}"));

        List<string> chunks = chunker.ChunkText(text);

        Assert.True(chunks.Count > 1);
        Assert.Contains("word", chunks[1]);
    }
}
