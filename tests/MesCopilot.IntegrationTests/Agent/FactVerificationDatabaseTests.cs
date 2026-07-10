using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Application.Services.Verification;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Entities.Products;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Data.Interceptors;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testcontainers.PostgreSql;

namespace MesCopilot.IntegrationTests.Agent;

public sealed class FactVerificationDatabaseTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg16")
        .WithDatabase("mes_verification")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task DelayedOrderCount_UsesTenantFilteredPrimaryDatabaseQuery()
    {
        await using MesDbContext context = CreateContext();
        await context.Database.MigrateAsync();
        Product product = new() { Code = "P-V", Name = "Verified Product", Unit = "pcs" };
        ProductionLine line = new() { Code = "L-V", Name = "Verified Line" };
        context.WorkOrders.Add(new WorkOrder
        {
            Code = "WO-V",
            Product = product,
            ProductionLine = line,
            PlannedQuantity = 1,
            PlannedStartTime = new DateTime(2026, 7, 10, 8, 0, 0, DateTimeKind.Utc),
            PlannedEndTime = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc),
            Status = WorkOrderStatus.InProgress
        });
        await context.SaveChangesAsync();
        VerificationQueryService service = new(context, new UnusedEquipmentService(), new UnusedQualityService());

        int count = await service.GetDelayedOrderCountAsync(
            new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        Assert.Equal(1, count);
        Assert.Contains("Npgsql", context.Database.ProviderName, StringComparison.Ordinal);
    }

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

    private sealed class UnusedEquipmentService : IEquipmentService
    {
        public Task<IEnumerable<EquipmentDto>> GetAllAsync() => throw new NotSupportedException();
        public Task<EquipmentDto?> GetByIdAsync(int id) => throw new NotSupportedException();
        public Task<IEnumerable<EquipmentStatusDto>> GetStatusHistoryAsync(int equipmentId) => throw new NotSupportedException();
        public Task<IEnumerable<EquipmentAlarmDto>> GetAlarmsAsync(int equipmentId) => throw new NotSupportedException();
        public Task<OeeDto> CalculateOeeAsync(int equipmentId, DateTime date) => throw new NotSupportedException();
    }

    private sealed class UnusedQualityService : IQualityService
    {
        public Task<IEnumerable<QualityInspectionDto>> GetInspectionsAsync() => throw new NotSupportedException();
        public Task<QualityInspectionDto> CreateInspectionAsync(CreateQualityInspectionRequest request) => throw new NotSupportedException();
        public Task<BatchTraceDto> TraceBatchAsync(string batchNumber) => throw new NotSupportedException();
        public Task<IEnumerable<DefectAnalysisDto>> AnalyzeDefectsAsync() => throw new NotSupportedException();
    }
}
