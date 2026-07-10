using MesCopilot.Agent.Prompts;

namespace MesCopilot.UnitTests.Agent.Prompts;

public sealed class LocalizedPromptBuilderTests
{
    [Theory]
    [InlineData("production")]
    [InlineData("quality")]
    [InlineData("oee")]
    [InlineData("knowledge")]
    public void BuildSystemPrompt_ReturnsDistinctChineseAndEnglishTemplates(string mode)
    {
        MesPromptBuilder builder = new();

        string chinese = builder.BuildSystemPrompt(mode, "zh-CN");
        string english = builder.BuildSystemPrompt(mode, "en-US");

        Assert.NotEqual(chinese, english);
        Assert.Contains("MES", chinese, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MES", english, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildUserPrompt_PreservesOriginalManufacturingQuestion()
    {
        MesPromptBuilder builder = new();
        const string question = "查询批次 B20260710-A102 的 OEE";

        string prompt = builder.BuildUserPrompt(question, context: null, locale: "en-US");

        Assert.Contains(question, prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSystemPrompt_WhenLocaleUnknown_FallsBackToChinese()
    {
        MesPromptBuilder builder = new();

        Assert.Equal(
            builder.BuildSystemPrompt("production", "zh-CN"),
            builder.BuildSystemPrompt("production", "fr-FR"));
    }
}
