namespace MesCopilot.Infrastructure.DocumentParsers;

public interface IDocumentParser
{
    bool CanParse(string fileName, string mimeType);

    Task<ParsedDocument> ParseAsync(Stream stream, CancellationToken cancellationToken = default);
}
