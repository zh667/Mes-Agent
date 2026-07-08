namespace MesCopilot.Domain.Entities.Production;

/// <summary>
/// 生产线。
/// </summary>
public class ProductionLine
{
    public int Id { get; set; }

    /// <summary>
    /// 产线编号。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 产线名称。
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

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间。
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
