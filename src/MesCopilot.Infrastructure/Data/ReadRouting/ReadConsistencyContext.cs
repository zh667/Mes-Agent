namespace MesCopilot.Infrastructure.Data.ReadRouting;

public sealed class ReadConsistencyContext : IReadConsistencyContext
{
    private DateTimeOffset _requirePrimaryUntil;

    public bool IsPrimaryRequired => DateTimeOffset.UtcNow < _requirePrimaryUntil;

    public void RequirePrimary(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        DateTimeOffset candidate = DateTimeOffset.UtcNow.Add(duration);
        if (candidate > _requirePrimaryUntil)
        {
            _requirePrimaryUntil = candidate;
        }
    }
}
