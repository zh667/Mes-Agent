using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Data.Interceptors;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.UnitTests.Infrastructure.Tenancy;

internal static class TenantTestDbContextFactory
{
    public const string TenantId = "00000000-0000-0000-0000-000000000099";

    public static MesDbContext Create(string? databaseName = null)
    {
        CurrentTenantContext tenantContext = new();
        tenantContext.Initialize(new TenantResolution(TenantId, IsPlatformAdmin: false));
        TenantWriteGuardInterceptor interceptor = new(tenantContext);
        DbContextOptions<MesDbContext> options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        return new MesDbContext(options, tenantContext);
    }
}
