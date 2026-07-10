namespace MesCopilot.Agent.Prompts;

public interface IPromptBuilder
{
    string BuildSystemPrompt(string agentMode);

    string BuildUserPrompt(string question, Dictionary<string, object>? context = null);
}
