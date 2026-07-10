namespace MesCopilot.Agent.Prompts;

public interface IPromptBuilder
{
    string BuildSystemPrompt(string agentMode);

    string BuildSystemPrompt(string agentMode, string locale);

    string BuildUserPrompt(string question, Dictionary<string, object>? context = null);

    string BuildUserPrompt(
        string question,
        Dictionary<string, object>? context,
        string locale);
}
