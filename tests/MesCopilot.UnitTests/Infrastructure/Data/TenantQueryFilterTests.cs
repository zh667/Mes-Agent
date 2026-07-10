using MesCopilot.Domain.Entities.Conversations;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Entities.Knowledge;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Data.Interceptors;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using EquipmentEntity = MesCopilot.Domain.Entities.Equipment.Equipment;

namespace MesCopilot.UnitTests.Infrastructure.Data;

public class TenantQueryFilterTests
{
    private const string TenantA = "00000000-0000-0000-0000-00000000000a";
    private const string TenantB = "00000000-0000-0000-0000-00000000000b";

    [Fact]
    public async Task Queries_ReturnOnlyCurrentTenantBusinessData()
    {
        string databaseName = Guid.NewGuid().ToString();
        await SeedTenantAsync(databaseName, TenantA, "A");
        await SeedTenantAsync(databaseName, TenantB, "B");

        await using MesDbContext context = CreateContext(databaseName, TenantA);

        Assert.Equal("WO-A", (await context.WorkOrders.SingleAsync()).Code);
        Assert.Equal("EQ-A", (await context.Equipment.SingleAsync()).Code);
        Assert.Equal("SOP-A", (await context.Documents.SingleAsync()).Title);
        Assert.Equal("Conversation A", (await context.Conversations.SingleAsync()).Title);
    }

    [Fact]
    public async Task Queries_WithoutTenantContext_ReturnNoBusinessData()
    {
        string databaseName = Guid.NewGuid().ToString();
        await SeedTenantAsync(databaseName, TenantA, "A");
        CurrentTenantContext tenantContext = new();
        DbContextOptions<MesDbContext> options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        await using MesDbContext context = new(options, tenantContext);

        Assert.Empty(await context.WorkOrders.ToListAsync());
        Assert.Empty(await context.Equipment.ToListAsync());
        Assert.Empty(await context.Documents.ToListAsync());
    }

    private static async Task SeedTenantAsync(string databaseName, string tenantId, string suffix)
    {
        await using MesDbContext context = CreateContext(databaseName, tenantId);
        context.WorkOrders.Add(new WorkOrder
        {
            Code = $"WO-{suffix}",
            PlannedQuantity = 1,
            PlannedStartTime = DateTime.UtcNow,
            PlannedEndTime = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        });
        context.Equipment.Add(new EquipmentEntity
        {
            Code = $"EQ-{suffix}",
            Name = $"Equipment {suffix}",
            CreatedAt = DateTime.UtcNow
        });
        context.Documents.Add(new Document
        {
            Title = $"SOP-{suffix}",
            FileName = $"sop-{suffix}.txt",
            FilePath = $"uploads/sop-{suffix}.txt",
            MimeType = "text/plain",
            Type = DocumentType.Sop,
            UploadedAt = DateTime.UtcNow
        });
        context.Conversations.Add(new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = "test-user",
            Mode = AgentMode.Production,
            Title = $"Conversation {suffix}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private static MesDbContext CreateContext(string databaseName, string tenantId)
    {
        CurrentTenantContext tenantContext = new();
        tenantContext.Initialize(new TenantResolution(tenantId, IsPlatformAdmin: false));
        TenantWriteGuardInterceptor interceptor = new(tenantContext);
        DbContextOptions<MesDbContext> options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase(databaseName)
            .AddInterceptors(interceptor)
            .Options;

        return new MesDbContext(options, tenantContext);
    }
}
