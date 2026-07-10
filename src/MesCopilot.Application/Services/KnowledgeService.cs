using System.Globalization;
using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Entities.Knowledge;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Caching;
using MesCopilot.Infrastructure.DocumentParsers;
using MesCopilot.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Application.Services;

public class KnowledgeService : IKnowledgeService
{
    private const int EmbeddingBatchSize = 10;

    private readonly MesDbContext _context;
    private readonly IReadOnlyList<IDocumentParser> _documentParsers;
    private readonly TextChunker? _textChunker;
    private readonly IVectorStore? _vectorStore;
    private readonly ICacheScopeVersionStore? _cacheScopeVersions;

    public KnowledgeService(MesDbContext context)
    {
        _context = context;
        _documentParsers = [];
    }

    public KnowledgeService(
        MesDbContext context,
        IEnumerable<IDocumentParser> documentParsers,
        TextChunker textChunker,
        IVectorStore vectorStore,
        ICacheScopeVersionStore? cacheScopeVersions = null)
    {
        _context = context;
        _documentParsers = documentParsers.ToList();
        _textChunker = textChunker;
        _vectorStore = vectorStore;
        _cacheScopeVersions = cacheScopeVersions;
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
        await InvalidateKnowledgeCacheAsync();

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
        List<float[]> embeddings = await GenerateEmbeddingsInBatchesAsync(vectorStore, chunkTexts);
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
        await InvalidateKnowledgeCacheAsync();

        return ToDto(document);
    }

    private static async Task<List<float[]>> GenerateEmbeddingsInBatchesAsync(
        IVectorStore vectorStore,
        IReadOnlyList<string> chunkTexts)
    {
        List<float[]> embeddings = [];
        for (int index = 0; index < chunkTexts.Count; index += EmbeddingBatchSize)
        {
            List<string> batch = chunkTexts.Skip(index).Take(EmbeddingBatchSize).ToList();
            Task<float[]>[] embeddingTasks = batch
                .Select(chunkText => vectorStore.GenerateEmbeddingAsync(chunkText))
                .ToArray();
            embeddings.AddRange(await Task.WhenAll(embeddingTasks));
        }

        return embeddings;
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
        await InvalidateKnowledgeCacheAsync();
        return true;
    }

    public async Task<DocumentVersionDto> UploadNewVersionAsync(
        int documentId,
        Stream fileStream,
        string fileName,
        string changeNote,
        string uploadedById)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        string normalizedFileName = ValidateVersionFileName(fileName);
        if (string.IsNullOrWhiteSpace(uploadedById))
        {
            throw new ArgumentException("Uploader id is required.", nameof(uploadedById));
        }

        Document document = await _context.Documents.FirstOrDefaultAsync(item => item.Id == documentId)
            ?? throw new ArgumentException($"Document {documentId} not found.", nameof(documentId));
        List<DocumentVersion> versions = await _context.DocumentVersions
            .Where(version => version.DocumentId == documentId)
            .OrderBy(version => version.VersionNumber)
            .ToListAsync();

        Stream versionContent = fileStream;
        MemoryStream? bufferedContent = null;
        long originalPosition = 0;
        int nextVersionNumber = versions.Count == 0
            ? 1
            : versions.Max(version => version.VersionNumber) + 1;
        long fileSize;
        if (fileStream.CanSeek)
        {
            originalPosition = fileStream.Position;
            fileSize = fileStream.Length;
            fileStream.Position = 0;
        }
        else
        {
            bufferedContent = new MemoryStream();
            await fileStream.CopyToAsync(bufferedContent);
            fileSize = bufferedContent.Length;
            bufferedContent.Position = 0;
            versionContent = bufferedContent;
        }

        DateTime now = DateTime.UtcNow;
        string filePath = CreateVersionFilePath(documentId, nextVersionNumber, normalizedFileName);

        try
        {
            foreach (DocumentVersion version in versions)
            {
                version.IsActive = false;
            }

            DocumentVersion newVersion = new()
            {
                DocumentId = documentId,
                VersionNumber = nextVersionNumber,
                FileName = normalizedFileName,
                FilePath = filePath,
                FileSize = fileSize,
                UploadedById = uploadedById,
                UploadedAt = now,
                ChangeNote = changeNote.Trim(),
                IsActive = true
            };

            document.FileName = normalizedFileName;
            document.FilePath = filePath;
            document.FileSize = fileSize;
            document.UploadedAt = now;
            document.VectorizationStatus = "completed";

            _context.DocumentVersions.Add(newVersion);
            await PersistVersionFileAsync(filePath, versionContent);
            try
            {
                await ReplaceDocumentChunksAsync(document.Id, normalizedFileName, document.MimeType, versionContent, now);
            }
            catch (Exception)
            {
                document.VectorizationStatus = "failed";
            }

            await _context.SaveChangesAsync();
            await InvalidateKnowledgeCacheAsync();

            return ToVersionDto(newVersion);
        }
        finally
        {
            if (fileStream.CanSeek)
            {
                fileStream.Position = originalPosition;
            }

            bufferedContent?.Dispose();
        }
    }

    public async Task<IReadOnlyList<DocumentVersionDto>> GetVersionHistoryAsync(int documentId)
    {
        List<DocumentVersion> versions = await _context.DocumentVersions
            .AsNoTracking()
            .Where(version => version.DocumentId == documentId)
            .OrderByDescending(version => version.VersionNumber)
            .ToListAsync();

        return versions.Select(ToVersionDto).ToList();
    }

    public async Task RevertToVersionAsync(int documentId, int versionId)
    {
        Document document = await _context.Documents.FirstOrDefaultAsync(item => item.Id == documentId)
            ?? throw new ArgumentException($"Document {documentId} not found.", nameof(documentId));
        List<DocumentVersion> versions = await _context.DocumentVersions
            .Where(version => version.DocumentId == documentId)
            .ToListAsync();
        DocumentVersion targetVersion = versions.FirstOrDefault(version => version.Id == versionId)
            ?? throw new ArgumentException($"Version {versionId} not found for document {documentId}.", nameof(versionId));

        foreach (DocumentVersion version in versions)
        {
            version.IsActive = version.Id == versionId;
        }

        document.FileName = targetVersion.FileName;
        document.FilePath = targetVersion.FilePath;
        document.FileSize = targetVersion.FileSize;
        document.UploadedAt = targetVersion.UploadedAt;
        document.VectorizationStatus = "pending";

        List<DocumentChunk> chunks = await _context.DocumentChunks
            .Where(chunk => chunk.DocumentId == documentId)
            .ToListAsync();
        _context.DocumentChunks.RemoveRange(chunks);
        await _context.SaveChangesAsync();
        await InvalidateKnowledgeCacheAsync();
    }

    public async Task<IReadOnlyList<DocumentSearchResultDto>> SearchSimilarAsync(
        string query,
        int topK = 5,
        double similarityThreshold = 0.7)
    {
        string normalizedQuery = ValidateSearchArguments(query, topK, similarityThreshold);
        IVectorStore vectorStore = _vectorStore
            ?? throw new InvalidOperationException("Vector store is not configured.");

        List<DocumentChunk> chunks = await vectorStore.SearchSimilarAsync(
            normalizedQuery,
            topK,
            similarityThreshold);

        if (chunks.Count == 0)
        {
            return [];
        }

        List<int> documentIds = chunks
            .Select(chunk => chunk.DocumentId)
            .Distinct()
            .ToList();
        Dictionary<int, Document> documentsById = await _context.Documents
            .AsNoTracking()
            .Where(document => documentIds.Contains(document.Id))
            .ToDictionaryAsync(document => document.Id);

        List<DocumentSearchResultDto> results = [];
        foreach (DocumentChunk chunk in chunks)
        {
            if (!documentsById.TryGetValue(chunk.DocumentId, out Document? document))
            {
                continue;
            }

            results.Add(new DocumentSearchResultDto(
                chunk.Id,
                chunk.DocumentId,
                document.Title,
                document.FileName,
                document.Type,
                chunk.Sequence,
                chunk.Content,
                chunk.PageNumber,
                chunk.SectionTitle));
        }

        return results;
    }

    private Task InvalidateKnowledgeCacheAsync()
    {
        return _cacheScopeVersions is null || string.IsNullOrWhiteSpace(_context.CurrentTenantId)
            ? Task.CompletedTask
            : _cacheScopeVersions.InvalidateAsync(_context.CurrentTenantId, "knowledge");
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

    private static string ValidateSearchArguments(string query, int topK, double similarityThreshold)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Search query is required.", nameof(query));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topK);
        if (topK > KnowledgeSearchLimits.MaxTopK)
        {
            throw new ArgumentOutOfRangeException(nameof(topK), $"TopK must be less than or equal to {KnowledgeSearchLimits.MaxTopK}.");
        }

        if (similarityThreshold < 0d || similarityThreshold > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(similarityThreshold), "Similarity threshold must be in the range [0, 1].");
        }

        return query.Trim();
    }

    private static string ValidateVersionFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        return Path.GetFileName(fileName.Trim());
    }

    private async Task ReplaceDocumentChunksAsync(
        int documentId,
        string fileName,
        string mimeType,
        Stream content,
        DateTime createdAt)
    {
        if (_textChunker is null || _vectorStore is null || _documentParsers.Count == 0)
        {
            return;
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        IDocumentParser parser = _documentParsers.FirstOrDefault(item => item.CanParse(fileName, mimeType))
            ?? throw new NotSupportedException($"No document parser is registered for {fileName}.");
        ParsedDocument parsedDocument = await parser.ParseAsync(content);
        List<string> chunkTexts = _textChunker.ChunkText(parsedDocument.Text);
        List<float[]> embeddings = chunkTexts.Count == 0
            ? []
            : await GenerateEmbeddingsInBatchesAsync(_vectorStore, chunkTexts);
        List<DocumentChunk> existingChunks = await _context.DocumentChunks
            .Where(chunk => chunk.DocumentId == documentId)
            .ToListAsync();
        _context.DocumentChunks.RemoveRange(existingChunks);

        if (chunkTexts.Count == 0)
        {
            return;
        }

        List<DocumentChunk> chunks = chunkTexts
            .Select((chunkText, index) => new DocumentChunk
            {
                DocumentId = documentId,
                Sequence = index + 1,
                Content = chunkText,
                Vector = ToVectorLiteral(embeddings[index]),
                TokenCount = EstimateTokenCount(chunkText),
                CreatedAt = createdAt
            })
            .ToList();
        _context.DocumentChunks.AddRange(chunks);
    }

    private static async Task PersistVersionFileAsync(string filePath, Stream content)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await using FileStream output = File.Create(filePath);
        await content.CopyToAsync(output);

        if (content.CanSeek)
        {
            content.Position = 0;
        }
    }

    private static string CreateVersionFilePath(int documentId, int versionNumber, string fileName)
    {
        return $"uploads/knowledge/doc-{documentId}/v{versionNumber}/{fileName}";
    }

    private static DocumentVersionDto ToVersionDto(DocumentVersion version)
    {
        return new DocumentVersionDto(
            version.Id,
            version.VersionNumber,
            version.FileName,
            version.ChangeNote,
            version.UploadedById,
            version.UploadedAt,
            version.FileSize,
            version.IsActive);
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
