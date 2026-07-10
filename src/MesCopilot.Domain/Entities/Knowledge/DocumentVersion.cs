namespace MesCopilot.Domain.Entities.Knowledge;

public class DocumentVersion : MesCopilot.Domain.Common.ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;

    public int Id { get; set; }

    public int DocumentId { get; set; }

    public Document Document { get; set; } = null!;

    public int VersionNumber { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public string UploadedById { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }

    public string ChangeNote { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
