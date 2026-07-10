using MesCopilot.Domain.Entities.Products;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Data.Interceptors;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Testcontainers.PostgreSql;
using BomEntity = MesCopilot.Domain.Entities.Products.Bom;

namespace MesCopilot.IntegrationTests.Bom;

public sealed class BomExplosionDatabaseTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg16")
        .WithDatabase("mes_bom")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Database_RejectsBomItemWithBothMaterialAndChildBom()
    {
        CurrentTenantContext tenant = new();
        tenant.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: true));
        DbContextOptions<MesDbContext> options = new DbContextOptionsBuilder<MesDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .AddInterceptors(new TenantWriteGuardInterceptor(tenant))
            .Options;
        await using MesDbContext context = new(options, tenant);
        await context.Database.MigrateAsync();
        Product p1 = new() { Code = "P1", Name = "P1", Unit = "set" };
        Product p2 = new() { Code = "P2", Name = "P2", Unit = "set" };
        Material material = new() { Code = "M1", Name = "M1", Unit = "pcs" };
        BomEntity b1 = new() { Code = "B1", Product = p1 };
        BomEntity b2 = new() { Code = "B2", Product = p2 };
        context.AddRange(b1, b2, material);
        await context.SaveChangesAsync();
        context.BomItems.Add(new BomItem
        {
            BomId = b1.Id,
            MaterialId = material.Id,
            ChildBomId = b2.Id,
            Quantity = 1m,
            Unit = "pcs"
        });

        DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.IsType<PostgresException>(exception.InnerException);
    }
}
