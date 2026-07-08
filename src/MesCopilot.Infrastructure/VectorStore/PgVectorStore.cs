using System.Data;
using System.Globalization;
using MesCopilot.Domain.Entities.Knowledge;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MesCopilot.Infrastructure.VectorStore;

public class PgVectorStore : IVectorStore
{
    private readonly MesDbContext _context;
    private readonly IEmbeddingClient _embeddingClient;

    public PgVectorStore(MesDbContext context, IEmbeddingClient embeddingClient)
    {
        _context = context;
        _embeddingClient = embeddingClient;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Embedding text is required.", nameof(text));
        }

        return await _embeddingClient.GenerateEmbeddingAsync(text);
    }

    public async Task StoreChunkAsync(DocumentChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        if (string.IsNullOrWhiteSpace(chunk.Vector))
        {
            throw new ArgumentException("Chunk must have a vector before it can be stored.", nameof(chunk));
        }

        _context.DocumentChunks.Add(chunk);
        await _context.SaveChangesAsync();
    }

    public async Task<List<DocumentChunk>> SearchSimilarAsync(string query, int topK = 5, double similarityThreshold = 0.7)
    {
        ValidateSearchArguments(query, topK, similarityThreshold);

        float[] queryVector = await GenerateEmbeddingAsync(query);
        string vectorString = ToPgVectorLiteral(queryVector);
        List<DocumentChunk> chunks = new();

        // pgvector similarity search requires provider-specific SQL; parameters keep values safe.
        const string sql = """
            SELECT "Id", "DocumentId", "Sequence", "Content", "Vector", "TokenCount", "PageNumber", "SectionTitle", "CreatedAt",
                   1 - ("Vector"::vector <=> @vector::vector) AS "Similarity"
            FROM "DocumentChunks"
            WHERE "Vector" IS NOT NULL
            ORDER BY "Vector"::vector <=> @vector::vector
            LIMIT @topK
            """;

        var connection = _context.Database.GetDbConnection();
        bool shouldCloseConnection = connection.State == ConnectionState.Closed;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.Add(new NpgsqlParameter("vector", vectorString));
            command.Parameters.Add(new NpgsqlParameter("topK", topK));

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                double similarity = reader.GetDouble(reader.GetOrdinal("Similarity"));
                if (similarity < similarityThreshold)
                {
                    continue;
                }

                chunks.Add(ReadChunk(reader));
            }
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        return chunks;
    }

    private static void ValidateSearchArguments(string query, int topK, double similarityThreshold)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Search query is required.", nameof(query));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);
        if (similarityThreshold < 0d || similarityThreshold > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(similarityThreshold), "Similarity threshold must be in the range [0, 1].");
        }
    }

    private static string ToPgVectorLiteral(float[] vector)
    {
        return "[" + string.Join(",", vector.Select(value => value.ToString(CultureInfo.InvariantCulture))) + "]";
    }

    private static DocumentChunk ReadChunk(IDataRecord reader)
    {
        return new DocumentChunk
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
            Sequence = reader.GetInt32(reader.GetOrdinal("Sequence")),
            Content = reader.GetString(reader.GetOrdinal("Content")),
            Vector = reader.IsDBNull(reader.GetOrdinal("Vector")) ? null : reader.GetString(reader.GetOrdinal("Vector")),
            TokenCount = reader.GetInt32(reader.GetOrdinal("TokenCount")),
            PageNumber = reader.IsDBNull(reader.GetOrdinal("PageNumber")) ? null : reader.GetInt32(reader.GetOrdinal("PageNumber")),
            SectionTitle = reader.IsDBNull(reader.GetOrdinal("SectionTitle")) ? null : reader.GetString(reader.GetOrdinal("SectionTitle")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
