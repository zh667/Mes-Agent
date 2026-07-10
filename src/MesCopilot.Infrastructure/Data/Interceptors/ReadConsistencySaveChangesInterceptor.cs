using MesCopilot.Infrastructure.Data.ReadRouting;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MesCopilot.Infrastructure.Data.Interceptors;

public sealed class ReadConsistencySaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly TimeSpan StickyWindow = TimeSpan.FromSeconds(5);

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (result > 0 && eventData.Context is MesDbContext context)
        {
            context.ReadConsistencyContext?.RequirePrimary(StickyWindow);
        }

        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (result > 0 && eventData.Context is MesDbContext context)
        {
            context.ReadConsistencyContext?.RequirePrimary(StickyWindow);
        }

        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
