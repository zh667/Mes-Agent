using MesCopilot.Agent.Models;

namespace MesCopilot.Agent.Verification;

public interface IFactVerifier
{
    VerificationResult Verify(FunctionCallResult result);
}
