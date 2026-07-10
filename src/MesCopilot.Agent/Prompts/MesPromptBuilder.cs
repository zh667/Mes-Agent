using System.Collections.Concurrent;
using System.Text;

namespace MesCopilot.Agent.Prompts;

public sealed class MesPromptBuilder : IPromptBuilder
{
    private static readonly IReadOnlyDictionary<string, string> TemplateFiles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["production"] = "production_agent_template.txt",
            ["quality"] = "quality_agent_template.txt",
            ["oee"] = "oee_agent_template.txt",
            ["knowledge"] = "knowledge_agent_template.txt"
        };

    private readonly ConcurrentDictionary<string, Lazy<string>> _templates = new(StringComparer.OrdinalIgnoreCase);

    public string BuildSystemPrompt(string agentMode) => BuildSystemPrompt(agentMode, "en-US");

    public string BuildSystemPrompt(string agentMode, string locale)
    {
        if (string.IsNullOrWhiteSpace(agentMode))
        {
            throw new ArgumentException("Agent mode is required.", nameof(agentMode));
        }

        string normalizedMode = agentMode.Trim().ToLowerInvariant();
        if (!TemplateFiles.ContainsKey(normalizedMode))
        {
            throw new ArgumentException($"Unknown agent mode: {agentMode}", nameof(agentMode));
        }

        string normalizedLocale = NormalizeLocale(locale);
        return _templates.GetOrAdd(
            $"{normalizedLocale}:{normalizedMode}",
            _ => new Lazy<string>(
                () => LoadTemplate(normalizedLocale, normalizedMode),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    public string BuildUserPrompt(string question, Dictionary<string, object>? context = null)
        => BuildUserPrompt(question, context, "en-US");

    public string BuildUserPrompt(
        string question,
        Dictionary<string, object>? context,
        string locale)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question is required.", nameof(question));
        }

        bool chinese = NormalizeLocale(locale) == "zh-CN";
        StringBuilder builder = new();
        builder.AppendLine($"{(chinese ? "用户" : "User")}: {question}");
        if (context is { Count: > 0 })
        {
            builder.AppendLine();
            builder.AppendLine(chinese ? "上下文:" : "Context:");
            foreach (KeyValuePair<string, object> item in context)
            {
                builder.AppendLine($"- {item.Key}: {item.Value}");
            }
        }
        return builder.ToString();
    }

    private static string NormalizeLocale(string? locale)
        => string.Equals(locale, "en-US", StringComparison.OrdinalIgnoreCase) ? "en-US" : "zh-CN";

    private static string LoadTemplate(string locale, string mode)
    {
        string fileName = TemplateFiles[mode];
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "Prompts", "Templates", locale, fileName),
            Path.Combine("Prompts", "Templates", locale, fileName),
            Path.Combine(AppContext.BaseDirectory, "Prompts", "Templates", fileName),
            Path.Combine("Prompts", "Templates", fileName)
        ];
        string? path = candidates.FirstOrDefault(File.Exists);
        if (path is null)
        {
            throw new FileNotFoundException($"Prompt template is missing for locale '{locale}' and mode '{mode}'.", fileName);
        }
        return File.ReadAllText(path);
    }
}
