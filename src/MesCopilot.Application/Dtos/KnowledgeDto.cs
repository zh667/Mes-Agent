using MesCopilot.Domain.Enums;

namespace MesCopilot.Application.Dtos;

public record DocumentDto(
    int Id,
    string Title,
    string FileName,
    string FilePath,
    DocumentType Type,
    long FileSize,
    string MimeType,
    DateTime UploadedAt,
    string VectorizationStatus,
    string? Description
);

public record DocumentSearchResultDto(
    int ChunkId,
    int DocumentId,
    string DocumentTitle,
    string FileName,
    DocumentType DocumentType,
    int Sequence,
    string Content,
    int? PageNumber,
    string? SectionTitle
);

public record CreateDocumentRequest(
    string Title,
    string FileName,
    string FilePath,
    DocumentType Type,
    long FileSize,
    string MimeType,
    string? Description
);

public record UploadDocumentRequest(
    string Title,
    string FileName,
    string FilePath,
    DocumentType Type,
    long FileSize,
    string MimeType,
    string? Description,
    Stream Content
);
