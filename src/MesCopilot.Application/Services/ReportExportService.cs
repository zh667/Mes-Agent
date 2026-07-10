using MesCopilot.Application.Dtos;
using MesCopilot.Application.Dtos.Reports;
using MesCopilot.Application.Dtos.Auditing;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data.ReadRouting;

namespace MesCopilot.Application.Services;

public class ReportExportService : IReportExportService
{
    private const int MaxOeeDateRangeDays = 90;
    private const int MaxOeeEquipmentCount = 20;

    private readonly IWorkOrderService _workOrderService;
    private readonly IQualityService _qualityService;
    private readonly IEquipmentService _equipmentService;
    private readonly IReadDbContextFactory? _readFactory;

    public ReportExportService(
        IWorkOrderService workOrderService,
        IQualityService qualityService,
        IEquipmentService equipmentService,
        IReadDbContextFactory? readFactory = null,
        ReadRoutingOptions? options = null)
    {
        _workOrderService = workOrderService;
        _qualityService = qualityService;
        _equipmentService = equipmentService;
        _readFactory = options?.Enabled == true ? readFactory : null;
    }

    public async Task<byte[]> ExportProductionReportAsync(DateTime date, int? lineId, ReportFormat format)
    {
        if (_readFactory is not null)
        {
            return await _readFactory.ExecuteAsync(
                (context, _) => new ReportExportService(
                    new WorkOrderService(context),
                    new QualityService(context),
                    new EquipmentService(context)).ExportProductionReportAsync(date, lineId, format));
        }

        DateTime day = date.Date;
        DateTime nextDay = day.AddDays(1);
        List<WorkOrderDto> orders = (await _workOrderService.GetAllAsync())
            .Where(order =>
                order.PlannedStartTime >= day &&
                order.PlannedStartTime < nextDay &&
                (!lineId.HasValue || order.ProductionLineId == lineId.Value))
            .OrderBy(order => order.PlannedStartTime)
            .ToList();

        int completedOrders = orders.Count(order => order.Status == WorkOrderStatus.Completed || order.Status == WorkOrderStatus.Closed);
        int delayedOrders = orders.Count(IsDelayed);
        int totalPlannedQuantity = orders.Sum(order => order.PlannedQuantity);
        int totalActualQuantity = orders.Sum(order => order.CompletedQuantity);
        var report = new ProductionReportDto(
            day,
            lineId,
            orders.Select(order => order.ProductionLineName).FirstOrDefault() ?? string.Empty,
            orders.Count,
            completedOrders,
            delayedOrders,
            Divide(completedOrders, orders.Count),
            totalPlannedQuantity,
            totalActualQuantity,
            Divide(totalActualQuantity, totalPlannedQuantity),
            orders.Select(order => new ProductionReportLineItemDto(
                order.Code,
                order.ProductName,
                order.PlannedQuantity,
                order.CompletedQuantity,
                order.Status,
                order.PlannedEndTime,
                IsDelayed(order))).ToList());

        return format == ReportFormat.Excel
            ? ExcelReportGenerator.GenerateProductionReport(report)
            : PdfReportGenerator.GenerateProductionReport(report);
    }

    public async Task<byte[]> ExportQualityReportAsync(DateTime from, DateTime to, ReportFormat format)
    {
        if (_readFactory is not null)
        {
            return await _readFactory.ExecuteAsync(
                (context, _) => new ReportExportService(
                    new WorkOrderService(context),
                    new QualityService(context),
                    new EquipmentService(context)).ExportQualityReportAsync(from, to, format));
        }

        ValidateDateRange(from, to);
        DateTime fromDate = from.Date;
        DateTime toExclusive = to.Date.AddDays(1);
        List<QualityInspectionDto> inspections = (await _qualityService.GetInspectionsAsync())
            .Where(inspection => inspection.InspectionTime >= fromDate && inspection.InspectionTime < toExclusive)
            .OrderBy(inspection => inspection.InspectionTime)
            .ToList();
        List<DefectAnalysisDto> defectSummary = (await _qualityService.AnalyzeDefectsAsync()).ToList();

        int passed = inspections.Count(inspection => inspection.Status == InspectionStatus.Pass);
        int failed = inspections.Count(inspection => inspection.Status == InspectionStatus.Fail);
        int totalDefects = defectSummary.Sum(item => item.TotalQuantity);
        var report = new QualityReportDto(
            fromDate,
            to.Date,
            inspections.Count,
            passed,
            failed,
            Divide(passed, inspections.Count),
            totalDefects,
            inspections.Select(inspection => new QualityReportLineItemDto(
                inspection.Code,
                inspection.BatchNumber,
                inspection.WorkOrderId,
                inspection.InspectorName,
                inspection.InspectionTime,
                inspection.Status,
                inspection.FailedQuantity)).ToList(),
            defectSummary.Select(item => new DefectSummaryItemDto(
                item.DefectTypeName,
                item.TotalQuantity,
                Divide(item.TotalQuantity, totalDefects))).ToList());

        return format == ReportFormat.Excel
            ? ExcelReportGenerator.GenerateQualityReport(report)
            : PdfReportGenerator.GenerateQualityReport(report);
    }

    public async Task<byte[]> ExportOeeReportAsync(IReadOnlyList<int> equipmentIds, DateTime from, DateTime to, ReportFormat format)
    {
        if (_readFactory is not null)
        {
            return await _readFactory.ExecuteAsync(
                (context, _) => new ReportExportService(
                    new WorkOrderService(context),
                    new QualityService(context),
                    new EquipmentService(context)).ExportOeeReportAsync(equipmentIds, from, to, format));
        }

        ValidateDateRange(from, to);
        DateTime fromDate = from.Date;
        DateTime toDate = to.Date;
        ValidateOeeReportScope(equipmentIds, fromDate, toDate);

        List<EquipmentDto> equipment = (await _equipmentService.GetAllAsync()).ToList();
        List<int> ids = equipmentIds.Count > 0
            ? equipmentIds.Distinct().ToList()
            : equipment.Where(item => item.IsActive).Select(item => item.Id).ToList();
        ValidateOeeEquipmentCount(ids.Count);

        Dictionary<int, EquipmentDto> equipmentById = equipment
            .Where(item => ids.Contains(item.Id))
            .ToDictionary(item => item.Id);
        List<OeeReportLineItemDto> lineItems = [];

        for (DateTime day = fromDate; day <= toDate; day = day.AddDays(1))
        {
            foreach (int equipmentId in ids)
            {
                OeeDto oee = await _equipmentService.CalculateOeeAsync(equipmentId, day);
                lineItems.Add(new OeeReportLineItemDto(
                    equipmentId,
                    equipmentById.TryGetValue(equipmentId, out EquipmentDto? item) ? item.Name : $"Equipment {equipmentId}",
                    day,
                    oee.Availability,
                    oee.Performance,
                    oee.Quality,
                    oee.Oee,
                    oee.RunningMinutes,
                    oee.TotalOutput,
                    oee.QualifiedOutput));
            }
        }

        var report = new OeeReportDto(
            fromDate,
            toDate,
            ids,
            Average(lineItems.Select(item => item.Oee)),
            Average(lineItems.Select(item => item.Availability)),
            Average(lineItems.Select(item => item.Performance)),
            Average(lineItems.Select(item => item.Quality)),
            lineItems);

        return format == ReportFormat.Excel
            ? ExcelReportGenerator.GenerateOeeReport(report)
            : PdfReportGenerator.GenerateOeeReport(report);
    }

    public Task<byte[]> ExportAuditReportAsync(IReadOnlyList<AuditReportRowDto> rows, ReportFormat format)
    {
        return Task.FromResult(AuditReportGenerator.Generate(rows, format));
    }

    private static bool IsDelayed(WorkOrderDto order)
    {
        return order.PlannedEndTime < DateTime.UtcNow &&
            order.Status != WorkOrderStatus.Completed &&
            order.Status != WorkOrderStatus.Closed;
    }

    private static decimal Divide(decimal numerator, decimal denominator)
    {
        return denominator == 0m ? 0m : numerator / denominator;
    }

    private static decimal Average(IEnumerable<decimal> values)
    {
        List<decimal> list = values.ToList();
        return list.Count == 0 ? 0m : list.Average();
    }

    private static void ValidateDateRange(DateTime from, DateTime to)
    {
        if (from.Date > to.Date)
        {
            throw new ArgumentException("The start date must be before or equal to the end date.", nameof(from));
        }
    }

    private static void ValidateOeeReportScope(IReadOnlyList<int> equipmentIds, DateTime fromDate, DateTime toDate)
    {
        int dayCount = (toDate - fromDate).Days + 1;
        if (dayCount > MaxOeeDateRangeDays)
        {
            throw new ArgumentOutOfRangeException(nameof(toDate), $"OEE report date range cannot exceed {MaxOeeDateRangeDays} days.");
        }

        if (equipmentIds.Count > 0)
        {
            ValidateOeeEquipmentCount(equipmentIds.Distinct().Count());
        }
    }

    private static void ValidateOeeEquipmentCount(int equipmentCount)
    {
        if (equipmentCount > MaxOeeEquipmentCount)
        {
            throw new ArgumentOutOfRangeException(nameof(equipmentCount), $"OEE report cannot include more than {MaxOeeEquipmentCount} equipment items.");
        }
    }
}
