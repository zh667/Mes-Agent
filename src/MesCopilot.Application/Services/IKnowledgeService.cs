using MesCopilot.Application.Dtos;

namespace MesCopilot.Application.Services;

public interface IKnowledgeService
{
    Task<IEnumerable<DocumentDto>> GetAllAsync();

    Task<DocumentDto?> GetByIdAsync(int id);

    Task<DocumentDto> CreateDocumentAsync(CreateDocumentRequest request);

    Task<DocumentDto> UploadDocumentAsync(UploadDocumentRequest request);

    Task<bool> DeleteAsync(int id);
}
