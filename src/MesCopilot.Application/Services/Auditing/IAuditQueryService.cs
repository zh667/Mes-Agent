using MesCopilot.Application.Dtos.Auditing;

namespace MesCopilot.Application.Services.Auditing;

public interface IAuditQueryService
{
    Task<PagedAuditResultDto<AuditLogDto>> GetOperationsAsync(
        DateTime? from, DateTime? to, int skip, int take, CancellationToken cancellationToken = default);

    Task<PagedAuditResultDto<DataChangeLogDto>> GetChangesAsync(
        DateTime? from, DateTime? to, string? entityType, int skip, int take, CancellationToken cancellationToken = default);

    Task<PagedAuditResultDto<AgentAuditLogDto>> GetAgentAsync(
        DateTime? from, DateTime? to, string? status, int skip, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditReportRowDto>> GetReportRowsAsync(
        string category, DateTime from, DateTime to, int limit, CancellationToken cancellationToken = default);
}
