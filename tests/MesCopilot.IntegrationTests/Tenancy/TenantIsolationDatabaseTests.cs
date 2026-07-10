using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Data.Interceptors;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using EquipmentEntity = MesCopilot.Domain.Entities.Equipment.Equipment;

namespace MesCopilot.IntegrationTests.Tenancy;

public sealed class TenantIsolationDatabaseTests : IAsyncLifetime
{
    private const string TenantA = "00000000-0000-0000-0000-00000000000a";
    private const string TenantB = "00000000-0000-0000-0000-00000000000b";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg16")
        .WithDatabase("mescopilot_tenant_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Database_EnforcesTenantScopedQueriesUniquenessAndRelationships()
    {
        await using (MesDbContext migrationContext = CreateContext(CreateTenantContext(TenantA)))
        {
            await migrationContext.Database.MigrateAsync();
        }

        await AddTenantAndEquipmentAsync(TenantA, "EQ-SHARED");
        await AddTenantAndEquipmentAsync(TenantB, "EQ-SHARED");

        await using MesDbContext tenantAContext = CreateContext(CreateTenantContext(TenantA));
        EquipmentEntity tenantAEquipment = await tenantAContext.Equipment.SingleAsync();
        Assert.Equal(TenantA, tenantAEquipment.TenantId);

        await using NpgsqlConnection connection = new(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "EquipmentStatuses" ("TenantId", "EquipmentId", "State", "StartTime")
            VALUES (@tenantId, @equipmentId, 'Running', NOW());
            """;
        command.Parameters.AddWithValue("tenantId", TenantB);
        command.Parameters.AddWithValue("equipmentId", tenantAEquipment.Id);

        PostgresException exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
    }

    [Fact]
    public async Task Migration_BackfillsPhaseTwoDataAndMembershipWithoutPlatformAdminPromotion()
    {
        await using (MesDbContext phaseTwoContext = CreateContext(CreateTenantContext(TenantA)))
        {
            IMigrator migrator = phaseTwoContext.GetService<IMigrator>();
            await migrator.MigrateAsync("20260709143521_AddConversationFullTextSearch");
        }

        await using (NpgsqlConnection connection = new(_postgres.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO "Products" ("Code", "Name", "Unit", "CreatedAt")
                VALUES ('LEGACY-PRODUCT', 'Legacy Product', 'pcs', NOW());

                INSERT INTO "AspNetUsers" (
                    "Id", "DisplayName", "Role", "CreatedAt", "UserName", "NormalizedUserName",
                    "Email", "NormalizedEmail", "EmailConfirmed", "PhoneNumberConfirmed",
                    "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
                VALUES (
                    'legacy-admin', 'Legacy Admin', 'Admin', NOW(), 'legacy@example.com', 'LEGACY@EXAMPLE.COM',
                    'legacy@example.com', 'LEGACY@EXAMPLE.COM', TRUE, FALSE, FALSE, TRUE, 0);
                """;
            await command.ExecuteNonQueryAsync();
        }

        await using (MesDbContext upgradeContext = CreateContext(CreateTenantContext(TenantA)))
        {
            await upgradeContext.Database.MigrateAsync();
        }

        await using MesDbContext defaultTenantContext = CreateContext(CreateTenantContext(SeedData.DefaultTenantId));
        Assert.Equal(
            SeedData.DefaultTenantId,
            (await defaultTenantContext.Products.SingleAsync(product => product.Code == "LEGACY-PRODUCT")).TenantId);
        UserTenantMembership membership = await defaultTenantContext.UserTenantMemberships
            .SingleAsync(item => item.UserId == "legacy-admin");
        Assert.Equal(UserRole.Admin, membership.Role);
        Assert.False((await defaultTenantContext.Users.SingleAsync(user => user.Id == "legacy-admin")).IsPlatformAdmin);
    }

    private async Task AddTenantAndEquipmentAsync(string tenantId, string equipmentCode)
    {
        CurrentTenantContext tenantContext = CreateTenantContext(tenantId);
        await using MesDbContext context = CreateContext(tenantContext);
        context.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Code = tenantId == TenantA ? "TENANT-A" : "TENANT-B",
            Name = tenantId == TenantA ? "Tenant A" : "Tenant B"
        });
        context.Equipment.Add(new EquipmentEntity
        {
            Code = equipmentCode,
            Name = equipmentCode,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private MesDbContext CreateContext(CurrentTenantContext tenantContext)
    {
        TenantWriteGuardInterceptor interceptor = new(tenantContext);
        DbContextOptions<MesDbContext> options = new DbContextOptionsBuilder<MesDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .AddInterceptors(interceptor)
            .Options;
        return new MesDbContext(options, tenantContext);
    }

    private static CurrentTenantContext CreateTenantContext(string tenantId)
    {
        CurrentTenantContext tenantContext = new();
        tenantContext.Initialize(new TenantResolution(tenantId, IsPlatformAdmin: false));
        return tenantContext;
    }
}
