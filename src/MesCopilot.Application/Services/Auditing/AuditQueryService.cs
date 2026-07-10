using MesCopilot.Application.Dtos.Auditing;
using MesCopilot.Domain.Entities.Auditing;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Data.ReadRouting;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Application.Services.Auditing;

public sealed class AuditQueryService : IAuditQueryService
{
    private readonly MesDbContext _context;
    private readonly IReadDbContextFactory? _readFactory;

    public AuditQueryService(
        MesDbContext context,
        IReadDbContextFactory? readFactory = null,
        ReadRoutingOptions? options = null)
    {
        _context = context;
        _readFactory = options?.Enabled == true ? readFactory : null;
    }

    public async Task<PagedAuditResultDto<AuditLogDto>> GetOperationsAsync(
        DateTime? from, DateTime? to, int skip, int take, CancellationToken cancellationToken = default)
    {
        if (_readFactory is not null)
        {
            return await _readFactory.ExecuteAsync(
                (context, token) => new AuditQueryService(context).GetOperationsAsync(from, to, skip, take, token),
                cancellationToken);
        }

        IQueryable<AuditLog> query = ApplyDateRange(_context.AuditLogs.AsNoTracking(), from, to);
        int total = await query.CountAsync(cancellationToken);
        List<AuditLogDto> items = await query
            .OrderByDescending(item => item.Timestamp)
            .Skip(skip)
            .Take(take)
            .Select(item => new AuditLogDto(
                item.Id, item.Timestamp, item.UserId, item.Method, item.RouteTemplate, item.QueryKeys,
                item.StatusCode, item.DurationMilliseconds, item.RequestBody, item.CorrelationId, item.IpHash))
            .ToListAsync(cancellationToken);
        return new PagedAuditResultDto<AuditLogDto>(items, total, skip, take);
    }

    public async Task<PagedAuditResultDto<DataChangeLogDto>> GetChangesAsync(
        DateTime? from, DateTime? to, string? entityType, int skip, int take, CancellationToken cancellationToken = default)
    {
        if (_readFactory is not null)
        {
            return await _readFactory.ExecuteAsync(
                (context, token) => new AuditQueryService(context).GetChangesAsync(from, to, entityType, skip, take, token),
                cancellationToken);
        }

        IQueryable<DataChangeLog> query = ApplyDateRange(_context.DataChangeLogs.AsNoTracking(), from, to);
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(item => item.EntityType == entityType);
        }

        int total = await query.CountAsync(cancellationToken);
        List<DataChangeLogDto> items = await query
            .OrderByDescending(item => item.Timestamp)
            .Skip(skip)
            .Take(take)
            .Select(item => new DataChangeLogDto(
                item.Id, item.Timestamp, item.UserId, item.EntityType, item.EntityId, item.ChangeType,
                item.OldValues, item.NewValues, item.CorrelationId))
            .ToListAsync(cancellationToken);
        return new PagedAuditResultDto<DataChangeLogDto>(items, total, skip, take);
    }

    public async Task<PagedAuditResultDto<AgentAuditLogDto>> GetAgentAsync(
        DateTime? from, DateTime? to, string? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        if (_readFactory is not null)
        {
            return await _readFactory.ExecuteAsync(
                (context, token) => new AuditQueryService(context).GetAgentAsync(from, to, status, skip, take, token),
                cancellationToken);
        }

        IQueryable<AgentAuditLog> query = ApplyDateRange(_context.AgentAuditLogs.AsNoTracking(), from, to);
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == status);
        }

        int total = await query.CountAsync(cancellationToken);
        List<AgentAuditLogDto> items = await query
            .OrderByDescending(item => item.Timestamp)
            .Skip(skip)
            .Take(take)
            .Select(item => new AgentAuditLogDto(
                item.Id, item.Timestamp, item.UserId, item.ConversationId, item.Mode, item.Status,
                item.ToolName, item.QueryDigest, item.IsVerified, item.Discrepancies,
                item.DurationMilliseconds, item.CorrelationId))
            .ToListAsync(cancellationToken);
        return new PagedAuditResultDto<AgentAuditLogDto>(items, total, skip, take);
    }

    public async Task<IReadOnlyList<AuditReportRowDto>> GetReportRowsAsync(
        string category, DateTime from, DateTime to, int limit, CancellationToken cancellationToken = default)
    {
        if (_readFactory is not null)
        {
            return await _readFactory.ExecuteAsync(
                (context, token) => new AuditQueryService(context).GetReportRowsAsync(category, from, to, limit, token),
                cancellationToken);
        }

        DateTime exclusiveTo = to.Date.AddDays(1);
        return category.ToLowerInvariant() switch
        {
            "operations" => await _context.AuditLogs.AsNoTracking()
                .Where(item => item.Timestamp >= from.Date && item.Timestamp < exclusiveTo)
                .OrderByDescending(item => item.Timestamp)
                .Take(limit)
                .Select(item => new AuditReportRowDto(
                    item.Timestamp, "Operation", item.UserId ?? "anonymous", item.Method,
                    item.RouteTemplate, item.StatusCode.ToString(), item.DurationMilliseconds))
                .ToListAsync(cancellationToken),
            "changes" => await _context.DataChangeLogs.AsNoTracking()
                .Where(item => item.Timestamp >= from.Date && item.Timestamp < exclusiveTo)
                .OrderByDescending(item => item.Timestamp)
                .Take(limit)
                .Select(item => new AuditReportRowDto(
                    item.Timestamp, "Change", item.UserId ?? "system", item.ChangeType,
                    item.EntityType + ":" + item.EntityId, "Recorded", 0))
                .ToListAsync(cancellationToken),
            "agent" => await _context.AgentAuditLogs.AsNoTracking()
                .Where(item => item.Timestamp >= from.Date && item.Timestamp < exclusiveTo)
                .OrderByDescending(item => item.Timestamp)
                .Take(limit)
                .Select(item => new AuditReportRowDto(
                    item.Timestamp, "Agent", item.UserId, item.ToolName ?? item.Mode.ToString(),
                    item.ConversationId.HasValue ? item.ConversationId.Value.ToString() : string.Empty,
                    item.Status, item.DurationMilliseconds))
                .ToListAsync(cancellationToken),
            _ => throw new ArgumentException("Unknown audit category.", nameof(category))
        };
    }

    private static IQueryable<T> ApplyDateRange<T>(IQueryable<T> query, DateTime? from, DateTime? to)
        where T : class
    {
        if (typeof(T) == typeof(AuditLog))
        {
            IQueryable<AuditLog> typed = (IQueryable<AuditLog>)query;
            if (from.HasValue) typed = typed.Where(item => item.Timestamp >= from.Value.Date);
            if (to.HasValue) typed = typed.Where(item => item.Timestamp < to.Value.Date.AddDays(1));
            return (IQueryable<T>)typed;
        }

        if (typeof(T) == typeof(DataChangeLog))
        {
            IQueryable<DataChangeLog> typed = (IQueryable<DataChangeLog>)query;
            if (from.HasValue) typed = typed.Where(item => item.Timestamp >= from.Value.Date);
            if (to.HasValue) typed = typed.Where(item => item.Timestamp < to.Value.Date.AddDays(1));
            return (IQueryable<T>)typed;
        }

        IQueryable<AgentAuditLog> agent = (IQueryable<AgentAuditLog>)query;
        if (from.HasValue) agent = agent.Where(item => item.Timestamp >= from.Value.Date);
        if (to.HasValue) agent = agent.Where(item => item.Timestamp < to.Value.Date.AddDays(1));
        return (IQueryable<T>)agent;
    }
}
