using MesCopilot.Domain.Enums;

namespace MesCopilot.Domain.Entities.Knowledge;

/// <summary>
/// 知识库文档。
/// </summary>
public class Document
{
    public int Id { get; set; }

    /// <summary>
    /// 文档标题。
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 文件名。
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件路径。
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文档类型。
    /// </summary>
    public DocumentType Type { get; set; }

    /// <summary>
    /// 文件大小，单位字节。
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME 类型。
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// 上传时间。
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// 向量化状态。
    /// </summary>
    public string VectorizationStatus { get; set; } = "pending";

    /// <summary>
    /// 描述。
    /// </summary>
    public string? Description { get; set; }

    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
