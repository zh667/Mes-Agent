namespace MesCopilot.Infrastructure.Caching;

public interface ICacheHealthProbe
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
