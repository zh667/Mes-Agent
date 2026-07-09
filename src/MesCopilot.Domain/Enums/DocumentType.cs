namespace MesCopilot.Domain.Enums;

/// <summary>
/// 文档类型。
/// </summary>
public enum DocumentType
{
    /// <summary>
    /// 标准作业程序。
    /// </summary>
    Sop = 0,

    /// <summary>
    /// 维修手册。
    /// </summary>
    MaintenanceManual = 1,

    /// <summary>
    /// 工艺文件。
    /// </summary>
    ProcessDocument = 2,

    /// <summary>
    /// 异常处理规范。
    /// </summary>
    ExceptionHandling = 3
}
