using MesCopilot.Agent.Models;

namespace MesCopilot.UnitTests.Agent.Models;

public class FunctionCallResultTests
{
    [Fact]
    public void FunctionCallResult_ShouldAllowStructuredDataExplanationAndDebugInfo()
    {
        var result = new FunctionCallResult
        {
            Data = new { totalCount = 2 },
            Explanation = "Found 2 records.",
            Debug = new DebugInfo
            {
                SqlExecuted = "SELECT 1",
                ExecutionTime = "12ms",
                DataSource = "MesCopilot.Database",
                ToolsCalled = new List<string> { "TestTool" }
            }
        };

        Assert.NotNull(result.Data);
        Assert.Equal("Found 2 records.", result.Explanation);
        Assert.Equal("SELECT 1", result.Debug?.SqlExecuted);
        Assert.Contains("TestTool", result.Debug?.ToolsCalled ?? []);
    }
}
