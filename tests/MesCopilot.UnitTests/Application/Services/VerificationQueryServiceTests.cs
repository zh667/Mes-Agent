using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Application.Services.Verification;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Entities.Knowledge;
using MesCopilot.Domain.Entities.Products;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using EquipmentEntity = MesCopilot.Domain.Entities.Equipment.Equipment;

namespace MesCopilot.UnitTests.Application.Services;

public sealed class VerificationQueryServiceTests
{
    [Fact]
    public async Task GetDelayedOrderCountAsync_AppliesDateAndStateBoundaries()
    {
        await using MesDbContext context = CreateContext();
        Product product = new() { TenantId = SeedData.DefaultTenantId, Code = "P-V", Name = "Product", Unit = "pcs" };
        ProductionLine line = new() { TenantId = SeedData.DefaultTenantId, Code = "L-V", Name = "Line" };
        context.WorkOrders.AddRange(
            CreateOrder("MATCH", product, line, WorkOrderStatus.InProgress, new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc)),
            CreateOrder("DONE", product, line, WorkOrderStatus.Completed, new DateTime(2026, 7, 10, 13, 0, 0, DateTimeKind.Utc)),
            CreateOrder("OUTSIDE", product, line, WorkOrderStatus.InProgress, new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();
        VerificationQueryService service = new(context, new FakeEquipmentService(), new FakeQualityService());

        int count = await service.GetDelayedOrderCountAsync(
            new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetOeeAsync_WhenEquipmentExists_ReturnsServiceSnapshot()
    {
        await using MesDbContext context = CreateContext();
        context.Equipment.Add(new EquipmentEntity { TenantId = SeedData.DefaultTenantId, Code = "EQ-V", Name = "Equipment", IsActive = true });
        await context.SaveChangesAsync();
        FakeEquipmentService equipment = new() { Oee = 0.83m };
        VerificationQueryService service = new(context, equipment, new FakeQualityService());

        VerifiedOeeSnapshot? result = await service.GetOeeAsync(1, new DateTime(2026, 7, 10), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0.83m, result.Oee);
    }

    [Fact]
    public async Task GetBatchTraceAsync_WhenSourceMissing_ReturnsNull()
    {
        await using MesDbContext context = CreateContext();
        VerificationQueryService service = new(context, new FakeEquipmentService(), new FakeQualityService { ThrowNotFound = true });

        Assert.Null(await service.GetBatchTraceAsync("UNKNOWN", CancellationToken.None));
    }

    [Fact]
    public async Task GetExistingDocumentIdsAsync_ReturnsOnlyTenantVisibleDocuments()
    {
        await using MesDbContext context = CreateContext();
        context.Documents.Add(new Document { TenantId = SeedData.DefaultTenantId, Title = "SOP", FileName = "sop.pdf", FilePath = "sop.pdf", FileSize = 1, Type = DocumentType.Sop, UploadedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
        VerificationQueryService service = new(context, new FakeEquipmentService(), new FakeQualityService());

        IReadOnlySet<int> result = await service.GetExistingDocumentIdsAsync([1, 2], CancellationToken.None);

        Assert.Equal([1], result);
    }

    private static WorkOrder CreateOrder(string code, Product product, ProductionLine line, WorkOrderStatus status, DateTime end) => new()
    {
        Code = code,
        TenantId = SeedData.DefaultTenantId,
        Product = product,
        ProductionLine = line,
        PlannedQuantity = 1,
        PlannedStartTime = end.AddHours(-1),
        PlannedEndTime = end,
        Status = status
    };

    private static MesDbContext CreateContext()
    {
        CurrentTenantContext tenant = new();
        tenant.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: false));
        DbContextOptions<MesDbContext> options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase($"verification-{Guid.NewGuid()}")
            .Options;
        return new MesDbContext(options, tenant);
    }

    private sealed class FakeEquipmentService : IEquipmentService
    {
        public decimal Oee { get; init; }
        public Task<OeeDto> CalculateOeeAsync(int equipmentId, DateTime date) => Task.FromResult(new OeeDto(equipmentId, date, 1, 1, 1, Oee, 1, 1, 1));
        public Task<IEnumerable<EquipmentDto>> GetAllAsync() => throw new NotSupportedException();
        public Task<EquipmentDto?> GetByIdAsync(int id) => throw new NotSupportedException();
        public Task<IEnumerable<EquipmentStatusDto>> GetStatusHistoryAsync(int equipmentId) => throw new NotSupportedException();
        public Task<IEnumerable<EquipmentAlarmDto>> GetAlarmsAsync(int equipmentId) => throw new NotSupportedException();
    }

    private sealed class FakeQualityService : IQualityService
    {
        public bool ThrowNotFound { get; init; }
        public Task<BatchTraceDto> TraceBatchAsync(string batchNumber) => ThrowNotFound
            ? Task.FromException<BatchTraceDto>(new KeyNotFoundException())
            : Task.FromResult(new BatchTraceDto(batchNumber, [], []));
        public Task<IEnumerable<QualityInspectionDto>> GetInspectionsAsync() => throw new NotSupportedException();
        public Task<QualityInspectionDto> CreateInspectionAsync(CreateQualityInspectionRequest request) => throw new NotSupportedException();
        public Task<IEnumerable<DefectAnalysisDto>> AnalyzeDefectsAsync() => throw new NotSupportedException();
    }
}
