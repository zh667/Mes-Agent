using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.DocumentParsers;
using MesCopilot.Infrastructure.Identity;
using MesCopilot.Infrastructure.VectorStore;
using Microsoft.AspNetCore.Identity;
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
