using MesCopilot.Agent.Models;

namespace MesCopilot.Agent.Verification;

public interface IVerificationRule
{
    bool Supports(string toolName);

    Task<VerificationResult> VerifyAsync(
        FunctionCallResult result,
        VerificationContext context,
        CancellationToken cancellationToken);
}
