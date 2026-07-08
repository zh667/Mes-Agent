using System.IO.Compression;
using System.Text;
using MesCopilot.Infrastructure.DocumentParsers;

namespace MesCopilot.UnitTests.Infrastructure.DocumentParsers;

public class WordParserTests
{
    [Fact]
    public async Task ParseAsync_ShouldExtractParagraphText()
    {
        await using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            ZipArchiveEntry documentEntry = archive.CreateEntry("word/document.xml");
            await using Stream entryStream = documentEntry.Open();
            await using var writer = new StreamWriter(entryStream, Encoding.UTF8);
            await writer.WriteAsync("""
                <?xml version="1.0" encoding="UTF-8"?>
                <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:body>
                    <w:p><w:r><w:t>First paragraph</w:t></w:r></w:p>
                    <w:p><w:r><w:t>Second paragraph</w:t></w:r></w:p>
                  </w:body>
                </w:document>
                """);
        }

        stream.Position = 0;
        var parser = new WordParser();

        ParsedDocument parsed = await parser.ParseAsync(stream);

        Assert.Contains("First paragraph", parsed.Text);
        Assert.Contains("Second paragraph", parsed.Text);
        Assert.Single(parsed.Sections);
    }
}
