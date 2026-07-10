using MesCopilot.Application.Services.Scheduling;
using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Domain.Entities.Products;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Data.Interceptors;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testcontainers.PostgreSql;
using EquipmentEntity = MesCopilot.Domain.Entities.Equipment.Equipment;

namespace MesCopilot.IntegrationTests.Scheduling;

public sealed class SchedulingConcurrencyDatabaseTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg16")
        .WithDatabase("mes_scheduling")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task ConcurrentAdjustments_ToSameEquipmentInterval_AllowOnlyOneCommit()
    {
        (int firstId, uint firstVersion, int secondId, uint secondVersion, int targetId) = await SeedAsync();
        DateTime start = new(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
        DateTime end = start.AddHours(1);

        Task<Exception?> first = CaptureAdjustmentAsync(firstId, firstVersion, targetId, start, end);
        Task<Exception?> second = CaptureAdjustmentAsync(secondId, secondVersion, targetId, start, end);
        Exception?[] results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result is null);
        Assert.Single(results, result => result is ScheduleConflictException);
        await using MesDbContext context = CreateContext();
        Assert.Equal(1, await context.WorkOrderOperations.CountAsync(operation =>
            operation.EquipmentId == targetId &&
            operation.PlannedStartTime < end &&
            operation.PlannedEndTime > start));
    }

    private async Task<Exception?> CaptureAdjustmentAsync(
        int operationId,
        uint version,
        int equipmentId,
        DateTime start,
        DateTime end)
    {
        try
        {
            await using MesDbContext context = CreateContext();
            await new SequentialSchedulingService(context).AdjustOperationAsync(
                operationId,
                equipmentId,
                start,
                end,
                version);
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private async Task<(int FirstId, uint FirstVersion, int SecondId, uint SecondVersion, int TargetId)> SeedAsync()
    {
        await using MesDbContext context = CreateContext();
        await context.Database.MigrateAsync();
        ProductionLine line = new() { Code = "L-CON", Name = "Concurrency Line" };
        Workstation station = new() { Code = "WS-CON", Name = "Concurrency Station", ProductionLine = line };
        Product product = new() { Code = "P-CON", Name = "Concurrency Product", Unit = "pcs" };
        ProcessRoute route = new() { Code = "R-CON", Name = "Concurrency Route", Product = product };
        ProcessStep step = new() { Code = "S-CON", Name = "Concurrency Step", ProcessRoute = route, Workstation = station, Sequence = 1, StandardTime = 1m };
        EquipmentEntity sourceA = new() { Code = "EQ-CON-A", Name = "Source A", ProductionLine = line, Workstation = station };
        EquipmentEntity sourceB = new() { Code = "EQ-CON-B", Name = "Source B", ProductionLine = line, Workstation = station };
        EquipmentEntity target = new() { Code = "EQ-CON-T", Name = "Target", ProductionLine = line, Workstation = station };
        WorkOrder firstOrder = CreateOrder("WO-CON-A", product, line);
        WorkOrder secondOrder = CreateOrder("WO-CON-B", product, line);
        WorkOrderOperation first = CreateOperation(firstOrder, step, sourceA, 8);
        WorkOrderOperation second = CreateOperation(secondOrder, step, sourceB, 9);
        context.AddRange(first, second, target);
        await context.SaveChangesAsync();
        return (first.Id, first.Version, second.Id, second.Version, target.Id);
    }

    private static WorkOrder CreateOrder(string code, Product product, ProductionLine line) => new()
    {
        Code = code,
        Product = product,
        ProductionLine = line,
        PlannedQuantity = 1,
        PlannedStartTime = new DateTime(2026, 7, 10, 8, 0, 0, DateTimeKind.Utc),
        PlannedEndTime = new DateTime(2026, 7, 10, 18, 0, 0, DateTimeKind.Utc),
        Status = WorkOrderStatus.Scheduled
    };

    private static WorkOrderOperation CreateOperation(
        WorkOrder order,
        ProcessStep step,
        EquipmentEntity equipment,
        int hour) => new()
        {
            WorkOrder = order,
            ProcessStep = step,
            Equipment = equipment,
            Sequence = 1,
            PlannedStartTime = new DateTime(2026, 7, 10, hour, 0, 0, DateTimeKind.Utc),
            PlannedEndTime = new DateTime(2026, 7, 10, hour + 1, 0, 0, DateTimeKind.Utc)
        };

    private MesDbContext CreateContext()
    {
        CurrentTenantContext tenant = new();
        tenant.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: false));
        DbContextOptions<MesDbContext> options = new DbContextOptionsBuilder<MesDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .AddInterceptors(new TenantWriteGuardInterceptor(tenant))
            .Options;
        return new MesDbContext(options, tenant);
    }
}
