using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Application.Services.Verification;

public sealed class VerificationQueryService : IVerificationQueryService
{
    private readonly MesDbContext _context;
    private readonly IEquipmentService _equipmentService;
    private readonly IQualityService _qualityService;

    public VerificationQueryService(
        MesDbContext context,
        IEquipmentService equipmentService,
        IQualityService qualityService)
    {
        _context = context;
        _equipmentService = equipmentService;
        _qualityService = qualityService;
    }

    public Task<int> GetDelayedOrderCountAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        return _context.WorkOrders
            .AsNoTracking()
            .Where(order =>
                order.PlannedEndTime >= fromUtc &&
                order.PlannedEndTime < toUtc &&
                order.Status != WorkOrderStatus.Completed &&
                order.Status != WorkOrderStatus.Closed)
            .CountAsync(cancellationToken);
    }

    public async Task<VerifiedOeeSnapshot?> GetOeeAsync(
        int equipmentId,
        DateTime date,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await _context.Equipment.AsNoTracking().AnyAsync(item => item.Id == equipmentId, cancellationToken))
        {
            return null;
        }

        Dtos.OeeDto oee = await _equipmentService.CalculateOeeAsync(equipmentId, date.Date);
        return new VerifiedOeeSnapshot(equipmentId, date.Date, oee.Oee);
    }

    public async Task<VerifiedBatchTrace?> GetBatchTraceAsync(
        string batchNumber,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            Dtos.BatchTraceDto trace = await _qualityService.TraceBatchAsync(batchNumber);
            return new VerifiedBatchTrace(
                trace.BatchNumber,
                trace.ProductionReports.Count,
                trace.Inspections.Count);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    public async Task<IReadOnlySet<int>> GetExistingDocumentIdsAsync(
        IReadOnlyCollection<int> documentIds,
        CancellationToken cancellationToken)
    {
        int[] ids = await _context.Documents
            .AsNoTracking()
            .Where(document => documentIds.Contains(document.Id))
            .Select(document => document.Id)
            .ToArrayAsync(cancellationToken);
        return ids.ToHashSet();
    }
}
