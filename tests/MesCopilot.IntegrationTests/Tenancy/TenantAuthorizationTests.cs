using System.Net;
using System.Net.Http.Json;
using MesCopilot.Api.Dtos.Tenants;
using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;
using MesCopilot.IntegrationTests.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace MesCopilot.IntegrationTests.Tenancy;

public class TenantAuthorizationTests
{
    [Fact]
    public async Task BusinessApi_WithoutTenantHeader_ReturnsStableRequiredError()
    {
        await using TenantApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/workorders");
        TenantProblemDetails? problem = await response.Content.ReadFromJsonAsync<TenantProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("TENANT_REQUIRED", problem?.Code);
    }

    [Fact]
    public async Task BusinessApi_WithUnknownMembership_ReturnsStableForbiddenError()
    {
        await using TenantApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", TenantApiFactory.TenantB);

        HttpResponseMessage response = await client.GetAsync("/api/workorders");
        TenantProblemDetails? problem = await response.Content.ReadFromJsonAsync<TenantProblemDetails>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("TENANT_FORBIDDEN", problem?.Code);
    }

    [Fact]
    public async Task CurrentTenant_UsesMembershipRoleResolvedForRequest()
    {
        await using TenantApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", TenantApiFactory.TenantA);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/current");
        TenantSummaryDto? tenant = await response.Content.ReadFromJsonAsync<TenantSummaryDto>();

        response.EnsureSuccessStatusCode();
        Assert.Equal("TeamLead", tenant?.Role);
    }
}

internal sealed class TenantApiFactory : WebApplicationFactory<Program>
{
    public const string TenantA = "00000000-0000-0000-0000-00000000000a";
    public const string TenantB = "00000000-0000-0000-0000-00000000000b";

    private readonly InMemoryDatabaseRoot _databaseRoot = new();
    private readonly string _databaseName = $"mescopilot-tenant-api-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "ThisIsATestSecretKeyThatIsAtLeast32BytesLong!!",
                ["Jwt:Issuer"] = "MesCopilot",
                ["Jwt:Audience"] = "MesCopilotClient",
                ["Auth:AllowRegistration"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MesDbContext>>();
            services.AddDbContext<MesDbContext>((provider, options) =>
                options.UseInMemoryDatabase(_databaseName, _databaseRoot)
                    .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                    .AddInterceptors(provider.GetRequiredService<MesCopilot.Infrastructure.Data.Interceptors.TenantWriteGuardInterceptor>()));
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.AuthenticationScheme,
                _ => { });

            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();
            CurrentTenantContext tenantContext = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
            tenantContext.Initialize(new TenantResolution(TenantA, IsPlatformAdmin: true));
            MesDbContext context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            context.Tenants.AddRange(
                new Tenant { Id = TenantA, Code = "TENANT-A", Name = "Tenant A" },
                new Tenant { Id = TenantB, Code = "TENANT-B", Name = "Tenant B" });
            context.Users.Add(new AppUser
            {
                Id = TestAuthHandler.DefaultUserId,
                UserName = TestAuthHandler.DefaultEmail,
                NormalizedUserName = TestAuthHandler.DefaultEmail.ToUpperInvariant(),
                Email = TestAuthHandler.DefaultEmail,
                NormalizedEmail = TestAuthHandler.DefaultEmail.ToUpperInvariant(),
                DisplayName = "Test User",
                IsPlatformAdmin = false
            });
            context.UserTenantMemberships.Add(new UserTenantMembership
            {
                UserId = TestAuthHandler.DefaultUserId,
                TenantId = TenantA,
                Role = UserRole.TeamLead,
                IsActive = true
            });
            context.SaveChanges();
        });
    }
}
