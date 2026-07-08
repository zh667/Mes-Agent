namespace MesCopilot.Domain.Entities.Knowledge;

/// <summary>
/// 文档切片。
/// </summary>
public class DocumentChunk
{
    public int Id { get; set; }

    /// <summary>
    /// 文档 ID。
    /// </summary>
    public int DocumentId { get; set; }

    /// <summary>
    /// 切片序号。
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// 文本内容。
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 向量。
    /// </summary>
    public string? Vector { get; set; }

    /// <summary>
    /// Token 数量。
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    /// 页码。
    /// </summary>
    public int? PageNumber { get; set; }

    /// <summary>
    /// 章节标题。
    /// </summary>
    public string? SectionTitle { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public Document Document { get; set; } = null!;
}
