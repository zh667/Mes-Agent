using System.Text;
using MesCopilot.Infrastructure.DocumentParsers;

namespace MesCopilot.UnitTests.Infrastructure.DocumentParsers;

public class PdfParserTests
{
    [Theory]
    [InlineData("manual.pdf", "application/octet-stream")]
    [InlineData("manual.bin", "application/pdf")]
    public void CanParse_PdfFileOrMimeType_ShouldReturnTrue(string fileName, string mimeType)
    {
        var parser = new PdfParser();

        Assert.True(parser.CanParse(fileName, mimeType));
    }

    [Fact]
    public async Task ParseAsync_WithLiteralPdfText_ShouldExtractAndUnescapeFragments()
    {
        var parser = new PdfParser();
        await using var stream = new MemoryStream(
            Encoding.Latin1.GetBytes(@"1 0 obj (Stop \(line\) on A102) (Reset\\sensor) endobj"));

        ParsedDocument parsed = await parser.ParseAsync(stream);

        Assert.Contains("Stop (line) on A102", parsed.Text);
        Assert.Contains(@"Reset\sensor", parsed.Text);
        ParsedDocumentSection section = Assert.Single(parsed.Sections);
        Assert.Equal(1, section.PageNumber);
        Assert.Equal(parsed.Text, section.Content);
    }

    [Fact]
    public async Task ParseAsync_WithoutLiteralText_ShouldFallbackToRawContent()
    {
        var parser = new PdfParser();
        await using var stream = new MemoryStream(Encoding.Latin1.GetBytes("%PDF raw bytes"));

        ParsedDocument parsed = await parser.ParseAsync(stream);

        Assert.Equal("%PDF raw bytes", parsed.Text);
    }

    [Fact]
    public async Task ParseAsync_NullStream_ShouldThrow()
    {
        var parser = new PdfParser();

        await Assert.ThrowsAsync<ArgumentNullException>(() => parser.ParseAsync(null!));
    }
}
