namespace MesCopilot.Domain.Enums;

/// <summary>
/// 工单状态。
/// </summary>
public enum WorkOrderStatus
{
    /// <summary>
    /// 未排程。
    /// </summary>
    NotScheduled = 0,

    /// <summary>
    /// 已排程。
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// 生产中。
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// 暂停。
    /// </summary>
    Paused = 3,

    /// <summary>
    /// 完工。
    /// </summary>
    Completed = 4,

    /// <summary>
    /// 关闭。
    /// </summary>
    Closed = 5
}
