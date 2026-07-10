using MesCopilot.Agent.Models;

namespace MesCopilot.Agent.Verification;

public interface IFactVerifier
{
    Task<VerificationResult> VerifyAsync(
        FunctionCallResult result,
        VerificationContext context,
        CancellationToken cancellationToken = default);
}
