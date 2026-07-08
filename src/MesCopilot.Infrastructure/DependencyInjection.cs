using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMesCopilotInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MesDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'MesDatabase' is not configured.");
        }

        services.AddDbContext<MesDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
