using System.IO.Compression;
using System.Xml.Linq;

namespace MesCopilot.Infrastructure.DocumentParsers;

public class WordParser : IDocumentParser
{
    private static readonly XNamespace WordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public bool CanParse(string fileName, string mimeType)
    {
        return fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ParsedDocument> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        ZipArchiveEntry documentEntry = archive.GetEntry("word/document.xml")
            ?? throw new InvalidDataException("Word document is missing word/document.xml.");

        await using Stream documentStream = documentEntry.Open();
        XDocument document = await XDocument.LoadAsync(documentStream, LoadOptions.None, cancellationToken);
        List<string> paragraphs = document
            .Descendants(WordNamespace + "p")
            .Select(ReadParagraph)
            .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))
            .ToList();

        string text = string.Join(Environment.NewLine, paragraphs);
        return new ParsedDocument(
            text,
            [new ParsedDocumentSection(null, null, text)]);
    }

    private static string ReadParagraph(XElement paragraph)
    {
        return string.Concat(paragraph.Descendants(WordNamespace + "t").Select(text => text.Value)).Trim();
    }
}
