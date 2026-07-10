using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.DocumentParsers;
using MesCopilot.Infrastructure.Identity;
using MesCopilot.Infrastructure.Data.Interceptors;
using MesCopilot.Infrastructure.Auditing;
using MesCopilot.Infrastructure.Tenancy;
using MesCopilot.Infrastructure.Caching;
using MesCopilot.Infrastructure.Data.ReadRouting;
using MesCopilot.Infrastructure.Devices;
using MesCopilot.Infrastructure.VectorStore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMesCopilotInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName = "Development")
    {
        var connectionString = configuration.GetConnectionString("MesDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'MesDatabase' is not configured.");
        }

        services.AddScoped<CurrentTenantContext>();
        services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<CurrentTenantContext>());
        services.AddSingleton(_ => new TenantWriteGuardInterceptor());
        services.AddSingleton<DataChangeAuditInterceptor>();
        services.AddSingleton<ReadConsistencySaveChangesInterceptor>();
        services.AddScoped<AuditRequestContext>();
        services.AddSingleton(new AuditRedactor(
            configuration["Audit:IpHashKey"] ?? "development-audit-ip-hash-key"));
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IReadConsistencyContext, ReadConsistencyContext>();
        services.AddScoped<IReadDbContextFactory, ReadDbContextFactory>();
        services.AddSingleton(new ReadRoutingOptions
        {
            Enabled = configuration.GetValue("ReadRouting:Enabled", false),
            PrimaryConnectionString = connectionString,
            ReadConnectionString = configuration.GetConnectionString("MesReadDatabase")
        });

        string? redisConnection = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Connection string 'Redis' is required in Production.");
            }

            services.AddDistributedMemoryCache();
            services.AddSingleton<ICacheScopeVersionStore, InMemoryCacheScopeVersionStore>();
        }
        else
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
            services.AddSingleton<ICacheScopeVersionStore, DistributedCacheScopeVersionStore>();
        }

        services.AddSingleton<IDeviceStatusCache, RedisDeviceStatusCache>();
        services.AddSingleton<ICacheHealthProbe, DistributedCacheHealthProbe>();
        services.AddSingleton<IAccessTokenRevocationStore, RedisAccessTokenRevocationStore>();
        var dataProtection = services.AddDataProtection();
        string? keyPath = configuration["DataProtection:KeyPath"];
        if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(keyPath))
        {
            throw new InvalidOperationException("DataProtection:KeyPath is required in Production.");
        }
        if (!string.IsNullOrWhiteSpace(keyPath))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyPath));
        }
        services.AddSingleton<IDeviceConnectionSecretProtector, DataProtectionDeviceConnectionSecretProtector>();
        bool allowInsecureDeviceTransport = !string.Equals(
            environmentName,
            "Production",
            StringComparison.OrdinalIgnoreCase);
        services.AddSingleton<IDeviceConnectorFactory>(
            _ => new DeviceConnectorFactory(allowInsecureDeviceTransport));
        services.AddDbContext<MesDbContext>((provider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    provider.GetRequiredService<TenantWriteGuardInterceptor>(),
                    provider.GetRequiredService<DataChangeAuditInterceptor>(),
                    provider.GetRequiredService<ReadConsistencySaveChangesInterceptor>()));
        services.AddIdentityCore<AppUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<MesDbContext>();

        services.AddScoped<ITokenService, TokenService>();
        services.AddHttpClient<IEmbeddingClient, OpenAiEmbeddingClient>();
        services.AddScoped<IVectorStore, PgVectorStore>();
        services.AddSingleton<TextChunker>();
        services.AddSingleton<IDocumentParser, WordParser>();
        services.AddSingleton<IDocumentParser, PdfParser>();

        return services;
    }
}
