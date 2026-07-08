using System.Globalization;
using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Entities.Knowledge;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.DocumentParsers;
using MesCopilot.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Application.Services;

public class KnowledgeService : IKnowledgeService
{
    private readonly MesDbContext _context;
    private readonly IReadOnlyList<IDocumentParser> _documentParsers;
    private readonly TextChunker? _textChunker;
    private readonly IVectorStore? _vectorStore;

    public KnowledgeService(MesDbContext context)
    {
        _context = context;
        _documentParsers = [];
    }

    public KnowledgeService(
        MesDbContext context,
        IEnumerable<IDocumentParser> documentParsers,
        TextChunker textChunker,
        IVectorStore vectorStore)
    {
        _context = context;
        _documentParsers = documentParsers.ToList();
        _textChunker = textChunker;
        _vectorStore = vectorStore;
    }

    public async Task<IEnumerable<DocumentDto>> GetAllAsync()
    {
        var documents = await _context.Documents
            .AsNoTracking()
            .OrderByDescending(document => document.UploadedAt)
            .ToListAsync();

        return documents.Select(ToDto);
    }

    public async Task<DocumentDto?> GetByIdAsync(int id)
    {
        var document = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        return document is null ? null : ToDto(document);
    }

    public async Task<DocumentDto> CreateDocumentAsync(CreateDocumentRequest request)
    {
        var document = new Document
        {
            Title = request.Title,
            FileName = request.FileName,
            FilePath = request.FilePath,
            Type = request.Type,
            FileSize = request.FileSize,
            MimeType = request.MimeType,
            UploadedAt = DateTime.UtcNow,
            VectorizationStatus = "pending",
            Description = request.Description
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return ToDto(document);
    }

    public async Task<DocumentDto> UploadDocumentAsync(UploadDocumentRequest request)
    {
        ValidateUploadRequest(request);
        TextChunker textChunker = _textChunker
            ?? throw new InvalidOperationException("TextChunker is not configured.");
        IVectorStore vectorStore = _vectorStore
            ?? throw new InvalidOperationException("Vector store is not configured.");
        IDocumentParser parser = _documentParsers.FirstOrDefault(item => item.CanParse(request.FileName, request.MimeType))
            ?? throw new NotSupportedException($"No document parser is registered for {request.FileName}.");

        DateTime timestamp = DateTime.UtcNow;
        var document = new Document
        {
            Title = request.Title,
            FileName = request.FileName,
            FilePath = request.FilePath,
            Type = request.Type,
            FileSize = request.FileSize,
            MimeType = request.MimeType,
            UploadedAt = timestamp,
            VectorizationStatus = "processing",
            Description = request.Description
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        ParsedDocument parsedDocument = await parser.ParseAsync(request.Content);
        List<string> chunkTexts = textChunker.ChunkText(parsedDocument.Text);
        Task<float[]>[] embeddingTasks = chunkTexts
            .Select(chunkText => vectorStore.GenerateEmbeddingAsync(chunkText))
            .ToArray();
        float[][] embeddings = await Task.WhenAll(embeddingTasks);
        List<DocumentChunk> chunks = chunkTexts
            .Select((chunkText, index) => new DocumentChunk
            {
                DocumentId = document.Id,
                Sequence = index + 1,
                Content = chunkText,
                Vector = ToVectorLiteral(embeddings[index]),
                TokenCount = EstimateTokenCount(chunkText),
                CreatedAt = timestamp
            })
            .ToList();

        _context.DocumentChunks.AddRange(chunks);
        document.VectorizationStatus = "completed";
        await _context.SaveChangesAsync();

        return ToDto(document);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(item => item.Id == id);
        if (document is null)
        {
            return false;
        }

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();
        return true;
    }

    private static DocumentDto ToDto(Document document)
    {
        return new DocumentDto(
            document.Id,
            document.Title,
            document.FileName,
            document.FilePath,
            document.Type,
            document.FileSize,
            document.MimeType,
            document.UploadedAt,
            document.VectorizationStatus,
            document.Description);
    }

    private static void ValidateUploadRequest(UploadDocumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Document title is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException("File name is required.", nameof(request));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.FileSize);
        ArgumentNullException.ThrowIfNull(request.Content);
    }

    private static int EstimateTokenCount(string text)
    {
        return Math.Max(1, text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length);
    }

    private static string ToVectorLiteral(float[] embedding)
    {
        return "[" + string.Join(",", embedding.Select(value => value.ToString(CultureInfo.InvariantCulture))) + "]";
    }
}
