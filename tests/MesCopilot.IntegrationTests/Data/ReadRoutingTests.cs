using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Data.Interceptors;
using MesCopilot.Infrastructure.Data.ReadRouting;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace MesCopilot.IntegrationTests.Data;

public sealed class ReadRoutingTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _primary = new PostgreSqlBuilder("pgvector/pgvector:pg16")
        .WithDatabase("mes_primary")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly PostgreSqlContainer _replica = new PostgreSqlBuilder("pgvector/pgvector:pg16")
        .WithDatabase("mes_replica")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_primary.StartAsync(), _replica.StartAsync());
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(_primary.DisposeAsync().AsTask(), _replica.DisposeAsync().AsTask());
    }

    [Fact]
    public async Task Factory_RoutesReplicaReadsAndHonorsPrimaryStickyWindow()
    {
        CurrentTenantContext tenantContext = new();
        tenantContext.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: false));
        ReadConsistencyContext consistency = new();
        ReadDbContextFactory factory = new(
            new ReadRoutingOptions
            {
                Enabled = true,
                PrimaryConnectionString = _primary.GetConnectionString(),
                ReadConnectionString = _replica.GetConnectionString()
            },
            consistency,
            tenantContext,
            new TenantWriteGuardInterceptor(tenantContext),
            NullLogger<ReadDbContextFactory>.Instance);

        string replicaDatabase = await factory.ExecuteAsync(ReadDatabaseNameAsync);
        consistency.RequirePrimary(TimeSpan.FromSeconds(5));
        string primaryDatabase = await factory.ExecuteAsync(ReadDatabaseNameAsync);

        Assert.Equal("mes_replica", replicaDatabase);
        Assert.Equal("mes_primary", primaryDatabase);
    }

    private static async Task<string> ReadDatabaseNameAsync(MesDbContext context, CancellationToken cancellationToken)
    {
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT current_database()";
        return (string)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
