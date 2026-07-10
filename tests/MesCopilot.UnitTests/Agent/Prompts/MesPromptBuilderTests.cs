using MesCopilot.Agent.Prompts;

namespace MesCopilot.UnitTests.Agent.Prompts;

public class MesPromptBuilderTests
{
    private readonly MesPromptBuilder _builder = new();

    [Theory]
    [InlineData("Production")]
    [InlineData("Quality")]
    [InlineData("OEE")]
    [InlineData("Knowledge")]
    public void BuildSystemPrompt_WithValidMode_ReturnsTemplate(string mode)
    {
        string prompt = _builder.BuildSystemPrompt(mode);

        Assert.NotEmpty(prompt);
        Assert.Contains("Agent", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_WithInvalidMode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _builder.BuildSystemPrompt("InvalidMode"));
    }

    [Fact]
    public void BuildUserPrompt_WithoutContext_ReturnsQuestionOnly()
    {
        string prompt = _builder.BuildUserPrompt("How many work orders are delayed?");

        Assert.Contains("User: How many work orders are delayed?", prompt);
        Assert.DoesNotContain("Context:", prompt);
    }

    [Fact]
    public void BuildUserPrompt_WithContext_IncludesContextInfo()
    {
        Dictionary<string, object> context = new()
        {
            ["userId"] = "user-123",
            ["timestamp"] = "2026-07-09T10:00:00Z"
        };

        string prompt = _builder.BuildUserPrompt("How many work orders are delayed?", context);

        Assert.Contains("User: How many work orders are delayed?", prompt);
        Assert.Contains("Context:", prompt);
        Assert.Contains("userId: user-123", prompt);
        Assert.Contains("timestamp: 2026-07-09T10:00:00Z", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_Production_ContainsFewShotExamples()
    {
        string prompt = _builder.BuildSystemPrompt("Production");

        Assert.Contains("Few-Shot Examples", prompt);
        Assert.Contains("work order", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Chain-of-Thought", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_Quality_ContainsTraceBatchExample()
    {
        string prompt = _builder.BuildSystemPrompt("Quality");

        Assert.Contains("TraceBatch", prompt);
        Assert.Contains("batch", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildSystemPrompt_Oee_ContainsFormula()
    {
        string prompt = _builder.BuildSystemPrompt("OEE");

        Assert.Contains("Availability", prompt);
        Assert.Contains("Performance", prompt);
        Assert.Contains("Quality", prompt);
    }
}
