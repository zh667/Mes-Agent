namespace MesCopilot.Agent.Models;

public class FunctionCallResult
{
    public object? Data { get; set; }

    public string Explanation { get; set; } = string.Empty;

    public DebugInfo? Debug { get; set; }
}

public class DebugInfo
{
    public string? SqlExecuted { get; set; }

    public string? ExecutionTime { get; set; }

    public string? DataSource { get; set; }

    public List<string>? ToolsCalled { get; set; }
}
