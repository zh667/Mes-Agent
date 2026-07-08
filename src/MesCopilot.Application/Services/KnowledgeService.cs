using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Entities.Knowledge;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Application.Services;

public class KnowledgeService : IKnowledgeService
{
    private readonly MesDbContext _context;

    public KnowledgeService(MesDbContext context)
    {
        _context = context;
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
}
