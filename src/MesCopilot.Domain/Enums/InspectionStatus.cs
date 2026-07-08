namespace MesCopilot.Domain.Enums;

/// <summary>
/// 质检状态。
/// </summary>
public enum InspectionStatus
{
    /// <summary>
    /// 待检。
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 合格。
    /// </summary>
    Pass = 1,

    /// <summary>
    /// 不合格。
    /// </summary>
    Fail = 2
}
