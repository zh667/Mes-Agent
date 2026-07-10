using System.Text;

namespace MesCopilot.Agent.Prompts;

public class MesPromptBuilder : IPromptBuilder
{
    private static readonly (string Mode, string FileName)[] TemplateFiles =
    [
        ("production", "production_agent_template.txt"),
        ("quality", "quality_agent_template.txt"),
        ("oee", "oee_agent_template.txt"),
        ("knowledge", "knowledge_agent_template.txt")
    ];

    private readonly Lazy<Dictionary<string, string>> _templates = new(LoadTemplates, isThreadSafe: true);

    public string BuildSystemPrompt(string agentMode)
    {
        if (string.IsNullOrWhiteSpace(agentMode))
        {
            throw new ArgumentException("Agent mode is required.", nameof(agentMode));
        }

        string normalizedMode = agentMode.Trim().ToLowerInvariant();
        if (_templates.Value.TryGetValue(normalizedMode, out string? template))
        {
            return template;
        }

        throw new ArgumentException($"Unknown agent mode: {agentMode}", nameof(agentMode));
    }

    public string BuildUserPrompt(string question, Dictionary<string, object>? context = null)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question is required.", nameof(question));
        }

        StringBuilder builder = new();
        builder.AppendLine($"User: {question}");

        if (context is { Count: > 0 })
        {
            builder.AppendLine();
            builder.AppendLine("Context:");
            foreach (KeyValuePair<string, object> item in context)
            {
                builder.AppendLine($"- {item.Key}: {item.Value}");
            }
        }

        return builder.ToString();
    }

    private static Dictionary<string, string> LoadTemplates()
    {
        Dictionary<string, string> templates = new(StringComparer.OrdinalIgnoreCase);
        string templateDirectory = Path.Combine(AppContext.BaseDirectory, "Prompts", "Templates");

        foreach ((string mode, string fileName) in TemplateFiles)
        {
            string path = Path.Combine(templateDirectory, fileName);
            if (!File.Exists(path))
            {
                path = Path.Combine("Prompts", "Templates", fileName);
            }

            if (File.Exists(path))
            {
                templates[mode] = File.ReadAllText(path);
            }
        }

        return templates;
    }
}
