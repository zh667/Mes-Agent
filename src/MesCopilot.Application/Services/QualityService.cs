using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Entities.Quality;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Application.Services;

public class QualityService : IQualityService
{
    private readonly MesDbContext _context;

    public QualityService(MesDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<QualityInspectionDto>> GetInspectionsAsync()
    {
        var inspections = await _context.QualityInspections
            .AsNoTracking()
            .OrderByDescending(inspection => inspection.InspectionTime)
            .ToListAsync();

        return inspections.Select(ToDto);
    }

    public async Task<QualityInspectionDto> CreateInspectionAsync(CreateQualityInspectionRequest request)
    {
        var inspection = new QualityInspection
        {
            Code = request.Code,
            BatchNumber = request.BatchNumber,
            WorkOrderId = request.WorkOrderId,
            ProcessStepId = request.ProcessStepId,
            InspectorId = request.InspectorId,
            InspectorName = request.InspectorName,
            InspectedQuantity = request.InspectedQuantity,
            PassedQuantity = request.PassedQuantity,
            FailedQuantity = request.FailedQuantity,
            Status = request.Status,
            InspectionTime = DateTime.UtcNow,
            Remarks = request.Remarks,
            CreatedAt = DateTime.UtcNow
        };

        _context.QualityInspections.Add(inspection);
        await _context.SaveChangesAsync();

        return ToDto(inspection);
    }

    public async Task<BatchTraceDto> TraceBatchAsync(string batchNumber)
    {
        var reports = await _context.ProductionReports
            .AsNoTracking()
            .Where(report => report.BatchNumber == batchNumber)
            .OrderBy(report => report.Timestamp)
            .ToListAsync();

        var inspections = await _context.QualityInspections
            .AsNoTracking()
            .Where(inspection => inspection.BatchNumber == batchNumber)
            .OrderBy(inspection => inspection.InspectionTime)
            .ToListAsync();

        return new BatchTraceDto(
            batchNumber,
            reports.Select(report => new ProductionReportTraceDto(
                report.Id,
                report.BatchNumber,
                report.WorkOrderId,
                report.ProcessStepId,
                report.EquipmentId,
                report.OperatorId,
                report.OperatorName,
                report.Timestamp,
                report.Quantity,
                report.QualifiedQuantity)).ToList(),
            inspections.Select(ToDto).ToList());
    }

    public async Task<IEnumerable<DefectAnalysisDto>> AnalyzeDefectsAsync()
    {
        var records = await _context.DefectRecords
            .AsNoTracking()
            .Include(record => record.DefectType)
            .ToListAsync();

        return records
            .GroupBy(record => record.DefectType)
            .Select(group => new DefectAnalysisDto(
                group.Key.Id,
                group.Key.Code,
                group.Key.Name,
                group.Sum(record => record.Quantity)))
            .OrderByDescending(item => item.TotalQuantity)
            .ToList();
    }

    private static QualityInspectionDto ToDto(QualityInspection inspection)
    {
        return new QualityInspectionDto(
            inspection.Id,
            inspection.Code,
            inspection.BatchNumber,
            inspection.WorkOrderId,
            inspection.ProcessStepId,
            inspection.InspectorId,
            inspection.InspectorName,
            inspection.InspectedQuantity,
            inspection.PassedQuantity,
            inspection.FailedQuantity,
            inspection.Status,
            inspection.InspectionTime,
            inspection.Remarks);
    }
}
