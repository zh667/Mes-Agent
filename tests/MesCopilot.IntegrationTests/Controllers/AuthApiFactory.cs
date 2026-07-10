using MesCopilot.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace MesCopilot.IntegrationTests.Controllers;

public class AuthApiFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _databaseRoot = new();
    private readonly string _accessTokenExpirationMinutes;

    public AuthApiFactory()
        : this("15")
    {
    }

    private AuthApiFactory(string accessTokenExpirationMinutes)
    {
        _accessTokenExpirationMinutes = accessTokenExpirationMinutes;
    }

    public static AuthApiFactory WithAccessTokenExpiration(string accessTokenExpirationMinutes)
    {
        return new AuthApiFactory(accessTokenExpirationMinutes);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MesDatabase"] = "Host=localhost;Port=5432;Database=mes_copilot_test;Username=postgres;Password=CHANGE_ME",
                ["Jwt:Secret"] = "ThisIsATestSecretKeyThatIsAtLeast32BytesLong!!",
                ["Jwt:Issuer"] = "MesCopilot",
                ["Jwt:Audience"] = "MesCopilotClient",
                ["Jwt:AccessTokenExpirationMinutes"] = _accessTokenExpirationMinutes,
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["Auth:AllowRegistration"] = "true"
            });
        });
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MesDbContext>>();
            services.AddDbContext<MesDbContext>(options =>
                options.UseInMemoryDatabase("mes-copilot-auth-api", _databaseRoot)
                    .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));

            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();
            MesDbContext context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            SeedData.SeedAsync(context).GetAwaiter().GetResult();
        });
    }
}
