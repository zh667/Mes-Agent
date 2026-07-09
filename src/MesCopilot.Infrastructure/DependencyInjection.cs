using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.DocumentParsers;
using MesCopilot.Infrastructure.VectorStore;
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
        services.AddHttpClient<IEmbeddingClient, OpenAiEmbeddingClient>();
        services.AddScoped<IVectorStore, PgVectorStore>();
        services.AddSingleton<TextChunker>();
        services.AddSingleton<IDocumentParser, WordParser>();
        services.AddSingleton<IDocumentParser, PdfParser>();

        return services;
    }
}
