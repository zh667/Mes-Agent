namespace MesCopilot.Infrastructure.Data.ReadRouting;

public interface IReadDbContextFactory
{
    Task<T> ExecuteAsync<T>(
        Func<MesDbContext, CancellationToken, Task<T>> query,
        CancellationToken cancellationToken = default);
}
