namespace MesCopilot.Infrastructure.DocumentParsers;

public record ParsedDocument(
    string Text,
    IReadOnlyList<ParsedDocumentSection> Sections
);

public record ParsedDocumentSection(
    int? PageNumber,
    string? SectionTitle,
    string Content
);
