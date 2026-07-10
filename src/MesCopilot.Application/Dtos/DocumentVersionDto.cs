namespace MesCopilot.Application.Dtos;

public record DocumentVersionDto(
    int Id,
    int VersionNumber,
    string FileName,
    string ChangeNote,
    string UploadedById,
    DateTime UploadedAt,
    long FileSizeBytes,
    bool IsActive);
