using System.Text;
using System.Text.RegularExpressions;

namespace MesCopilot.Infrastructure.DocumentParsers;

public class PdfParser : IDocumentParser
{
    private static readonly Regex LiteralTextPattern = new(@"\((?<text>(?:\\.|[^\\)])*)\)", RegexOptions.Compiled);

    public bool CanParse(string fileName, string mimeType)
    {
        return fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Contains("pdf", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ParsedDocument> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        string rawContent = Encoding.Latin1.GetString(buffer.ToArray());
        List<string> textFragments = LiteralTextPattern
            .Matches(rawContent)
            .Select(match => UnescapePdfText(match.Groups["text"].Value))
            .Where(fragment => !string.IsNullOrWhiteSpace(fragment))
            .ToList();

        string text = textFragments.Count > 0
            ? string.Join(" ", textFragments)
            : rawContent;

        return new ParsedDocument(
            text,
            [new ParsedDocumentSection(1, null, text)]);
    }

    private static string UnescapePdfText(string text)
    {
        return text
            .Replace("\\(", "(", StringComparison.Ordinal)
            .Replace("\\)", ")", StringComparison.Ordinal)
            .Replace("\\\\", "\\", StringComparison.Ordinal)
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\\r", "\r", StringComparison.Ordinal)
            .Trim();
    }
}
