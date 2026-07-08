namespace MesCopilot.Domain.Entities.Quality;

/// <summary>
/// 不良类型。
/// </summary>
public class DefectType
{
    public int Id { get; set; }

    /// <summary>
    /// 类型编号。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 类型名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 描述。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool IsActive { get; set; } = true;

    public ICollection<DefectRecord> DefectRecords { get; set; } = new List<DefectRecord>();
}
