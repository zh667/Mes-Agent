using System.Net;
using System.Net.Http.Json;
using MesCopilot.Application.Dtos;
using MesCopilot.Infrastructure.Data;
using MesCopilot.IntegrationTests.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace MesCopilot.IntegrationTests.Controllers;

public class WorkOrdersControllerTests : IClassFixture<WorkOrdersApiFactory>
{
    private readonly HttpClient _client;

    public WorkOrdersControllerTests(WorkOrdersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ShouldReturnWorkOrders()
    {
        var response = await _client.GetAsync("/api/workorders");

        response.EnsureSuccessStatusCode();
        var workOrders = await response.Content.ReadFromJsonAsync<List<WorkOrderDto>>();
        Assert.NotNull(workOrders);
        Assert.NotEmpty(workOrders);
    }

    [Fact]
    public async Task GetById_ExistingId_ShouldReturnWorkOrder()
    {
        var response = await _client.GetAsync("/api/workorders/1");

        response.EnsureSuccessStatusCode();
        var workOrder = await response.Content.ReadFromJsonAsync<WorkOrderDto>();
        Assert.NotNull(workOrder);
        Assert.Equal(1, workOrder.Id);
    }

    [Fact]
    public async Task GetById_NonExistingId_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync("/api/workorders/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldIncludeResponseTimeHeader()
    {
        var response = await _client.GetAsync("/api/workorders");

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.Contains("X-Response-Time-ms"));
        string value = Assert.Single(response.Headers.GetValues("X-Response-Time-ms"));
        Assert.True(decimal.TryParse(value, out decimal elapsedMilliseconds));
        Assert.True(elapsedMilliseconds >= 0m);
    }
}

public class WorkOrdersApiFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

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
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MesDbContext>>();
            services.AddDbContext<MesDbContext>(options =>
                options.UseInMemoryDatabase("mes-copilot-api", _databaseRoot));
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme,
                    _ => { });

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            SeedData.SeedAsync(context).GetAwaiter().GetResult();
        });
    }
}
