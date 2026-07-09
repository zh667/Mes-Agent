namespace MesCopilot.Domain.Enums;

/// <summary>
/// 设备状态。
/// </summary>
public enum EquipmentState
{
    /// <summary>
    /// 运行。
    /// </summary>
    Running = 0,

    /// <summary>
    /// 待机。
    /// </summary>
    Idle = 1,

    /// <summary>
    /// 报警。
    /// </summary>
    Alarm = 2,

    /// <summary>
    /// 维修。
    /// </summary>
    Maintenance = 3,

    /// <summary>
    /// 离线。
    /// </summary>
    Offline = 4
}
