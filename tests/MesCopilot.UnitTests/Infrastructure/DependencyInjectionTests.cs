using MesCopilot.Infrastructure;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.UnitTests.Infrastructure;

public class DependencyInjectionTests
{
    [Fact]
    public void AddMesCopilotInfrastructure_ShouldRegisterMesDbContext()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MesDatabase"] = "Host=localhost;Port=5432;Database=mes_copilot;Username=postgres;Password=CHANGE_ME"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddMesCopilotInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetService<MesDbContext>();

        Assert.NotNull(context);
        Assert.True(context.Database.ProviderName?.Contains("Npgsql") == true);
    }

    [Fact]
    public void AddMesCopilotInfrastructure_ProductionWithoutDataProtectionKeyPath_ShouldFailFast()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MesDatabase"] = "Host=localhost;Database=mes_copilot;Username=postgres;Password=secret",
                ["ConnectionStrings:Redis"] = "localhost:6379,password=secret"
            })
            .Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddMesCopilotInfrastructure(configuration, "Production"));

        Assert.Contains("DataProtection:KeyPath", exception.Message, StringComparison.Ordinal);
    }
}
