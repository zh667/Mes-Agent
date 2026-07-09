using MesCopilot.Application.Dtos;

namespace MesCopilot.Application.Services;

public interface IKnowledgeService
{
    Task<IEnumerable<DocumentDto>> GetAllAsync();

    Task<DocumentDto?> GetByIdAsync(int id);

    Task<DocumentDto> CreateDocumentAsync(CreateDocumentRequest request);

    Task<DocumentDto> UploadDocumentAsync(UploadDocumentRequest request);

    Task<IReadOnlyList<DocumentSearchResultDto>> SearchSimilarAsync(
        string query,
        int topK = 5,
        double similarityThreshold = 0.7);

    Task<bool> DeleteAsync(int id);
}
