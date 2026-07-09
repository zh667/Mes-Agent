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
}
