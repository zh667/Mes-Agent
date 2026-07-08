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

public record CreateDocumentRequest(
    string Title,
    string FileName,
    string FilePath,
    DocumentType Type,
    long FileSize,
    string MimeType,
    string? Description
);
