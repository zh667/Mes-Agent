using MesCopilot.Domain.Entities.Production;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Data.Interceptors;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.UnitTests.Infrastructure.Tenancy;

public class TenantWriteGuardInterceptorTests
{
    private const string TenantA = "00000000-0000-0000-0000-00000000000a";
    private const string TenantB = "00000000-0000-0000-0000-00000000000b";

    [Fact]
    public async Task SaveChangesAsync_AddedTenantEntity_AssignsCurrentTenant()
    {
        CurrentTenantContext tenantContext = CreateTenantContext(TenantA);
        await using MesDbContext context = CreateContext(tenantContext);
        WorkOrder workOrder = CreateWorkOrder();

        context.WorkOrders.Add(workOrder);
        await context.SaveChangesAsync();

        Assert.Equal(TenantA, workOrder.TenantId);
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutTenantContext_RejectsBusinessWrite()
    {
        CurrentTenantContext tenantContext = new();
        await using MesDbContext context = CreateContext(tenantContext);
        context.WorkOrders.Add(CreateWorkOrder());

        await Assert.ThrowsAsync<TenantBoundaryViolationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_WhenTenantIdIsChanged_RejectsUpdate()
    {
        CurrentTenantContext tenantContext = CreateTenantContext(TenantA);
        await using MesDbContext context = CreateContext(tenantContext);
        WorkOrder workOrder = CreateWorkOrder();
        context.WorkOrders.Add(workOrder);
        await context.SaveChangesAsync();

        workOrder.TenantId = TenantB;

        await Assert.ThrowsAsync<TenantBoundaryViolationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_WhenDeletingAnotherTenantEntity_RejectsDelete()
    {
        CurrentTenantContext tenantAContext = CreateTenantContext(TenantA);
        await using MesDbContext seedContext = CreateContext(tenantAContext, "tenant-delete");
        WorkOrder workOrder = CreateWorkOrder();
        seedContext.WorkOrders.Add(workOrder);
        await seedContext.SaveChangesAsync();

        CurrentTenantContext tenantBContext = CreateTenantContext(TenantB);
        await using MesDbContext deleteContext = CreateContext(tenantBContext, "tenant-delete");
        WorkOrder crossTenantOrder = await deleteContext.WorkOrders
            .IgnoreQueryFilters()
            .SingleAsync();
        deleteContext.WorkOrders.Remove(crossTenantOrder);

        await Assert.ThrowsAsync<TenantBoundaryViolationException>(() => deleteContext.SaveChangesAsync());
    }

    [Fact]
    public void Initialize_WhenCalledTwice_RejectsTenantSwitch()
    {
        CurrentTenantContext tenantContext = CreateTenantContext(TenantA);

        Assert.Throws<InvalidOperationException>(() =>
            tenantContext.Initialize(new TenantResolution(TenantB, IsPlatformAdmin: false)));
    }

    private static CurrentTenantContext CreateTenantContext(string tenantId)
    {
        CurrentTenantContext tenantContext = new();
        tenantContext.Initialize(new TenantResolution(tenantId, IsPlatformAdmin: false));
        return tenantContext;
    }

    private static MesDbContext CreateContext(CurrentTenantContext tenantContext, string? databaseName = null)
    {
        TenantWriteGuardInterceptor interceptor = new(tenantContext);
        DbContextOptions<MesDbContext> options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        return new MesDbContext(options, tenantContext);
    }

    private static WorkOrder CreateWorkOrder()
    {
        return new WorkOrder
        {
            Code = $"WO-{Guid.NewGuid():N}",
            PlannedQuantity = 10,
            PlannedStartTime = DateTime.UtcNow,
            PlannedEndTime = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };
    }
}
