using MesCopilot.Infrastructure.Data.Interceptors;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MesCopilot.Infrastructure.Data.ReadRouting;

public sealed class ReadDbContextFactory : IReadDbContextFactory
{
    private readonly ReadRoutingOptions _options;
    private readonly IReadConsistencyContext _consistencyContext;
    private readonly ITenantContext _tenantContext;
    private readonly TenantWriteGuardInterceptor _writeGuard;
    private readonly ILogger<ReadDbContextFactory> _logger;

    public ReadDbContextFactory(
        ReadRoutingOptions options,
        IReadConsistencyContext consistencyContext,
        ITenantContext tenantContext,
        TenantWriteGuardInterceptor writeGuard,
        ILogger<ReadDbContextFactory> logger)
    {
        _options = options;
        _consistencyContext = consistencyContext;
        _tenantContext = tenantContext;
        _writeGuard = writeGuard;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<MesDbContext, CancellationToken, Task<T>> query,
        CancellationToken cancellationToken = default)
    {
        bool useReplica = _options.Enabled &&
            !_consistencyContext.IsPrimaryRequired &&
            !string.IsNullOrWhiteSpace(_options.ReadConnectionString);
        try
        {
            await using MesDbContext context = CreateContext(requirePrimary: !useReplica);
            return await query(context, cancellationToken);
        }
        catch (NpgsqlException exception) when (useReplica && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Replica read failed; retrying the idempotent query on primary.");
            await using MesDbContext fallback = CreateContext(requirePrimary: true);
            return await query(fallback, cancellationToken);
        }
    }

    public static string SelectConnectionString(ReadRoutingOptions options, bool requirePrimary)
    {
        if (!options.Enabled || requirePrimary || string.IsNullOrWhiteSpace(options.ReadConnectionString))
        {
            return options.PrimaryConnectionString;
        }

        return options.ReadConnectionString;
    }

    private MesDbContext CreateContext(bool requirePrimary)
    {
        string connectionString = SelectConnectionString(_options, requirePrimary);
        DbContextOptions<MesDbContext> options = new DbContextOptionsBuilder<MesDbContext>()
            .UseNpgsql(connectionString)
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .AddInterceptors(_writeGuard)
            .Options;
        return new MesDbContext(options, _tenantContext);
    }
}
