namespace MesCopilot.Infrastructure.Data.ReadRouting;

public interface IReadConsistencyContext
{
    bool IsPrimaryRequired { get; }

    void RequirePrimary(TimeSpan duration);
}
