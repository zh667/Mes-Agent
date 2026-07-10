using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MesCopilot.UnitTests.Application.Services;

public class ReportExportServiceTests
{
    [Fact]
    public async Task ExportProductionReportAsync_Excel_ReturnsXlsxPackage()
    {
        ReportExportService service = CreateService();

        byte[] file = await service.ExportProductionReportAsync(DateTime.UtcNow.Date, lineId: null, ReportFormat.Excel);

        Assert.StartsWith("PK", System.Text.Encoding.ASCII.GetString(file, 0, 2));
        Assert.True(file.Length > 1000);
    }

    [Fact]
    public async Task ExportOeeReportAsync_Pdf_ReturnsPdfDocument()
    {
        ReportExportService service = CreateService();

        byte[] file = await service.ExportOeeReportAsync([1], DateTime.UtcNow.Date, DateTime.UtcNow.Date, ReportFormat.Pdf);

        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(file, 0, 4));
        Assert.True(file.Length > 1000);
    }

    [Fact]
    public async Task ExportOeeReportAsync_Pdf_UsesLandscapePageForWideMetrics()
    {
        ReportExportService service = CreateService();

        byte[] file = await service.ExportOeeReportAsync([1], DateTime.UtcNow.Date, DateTime.UtcNow.Date, ReportFormat.Pdf);
        string pdf = System.Text.Encoding.Latin1.GetString(file);
        Match mediaBox = Regex.Match(pdf, @"/MediaBox\s*\[\s*0\s+0\s+(?<width>[\d.]+)\s+(?<height>[\d.]+)");

        Assert.True(mediaBox.Success, "PDF should declare a page MediaBox.");
        decimal width = decimal.Parse(mediaBox.Groups["width"].Value, CultureInfo.InvariantCulture);
        decimal height = decimal.Parse(mediaBox.Groups["height"].Value, CultureInfo.InvariantCulture);
        Assert.True(width > height, $"Expected landscape PDF, but MediaBox was {width}x{height}.");
    }

    [Fact]
    public async Task ExportOeeReportAsync_DateRangeAboveLimit_ShouldThrow()
    {
        ReportExportService service = CreateService();
        DateTime from = DateTime.UtcNow.Date;
        DateTime to = from.AddDays(90);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.ExportOeeReportAsync([1], from, to, ReportFormat.Excel));
    }

    [Fact]
    public async Task ExportOeeReportAsync_EquipmentIdsAboveLimit_ShouldThrow()
    {
        ReportExportService service = CreateService();
        int[] equipmentIds = Enumerable.Range(1, 21).ToArray();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.ExportOeeReportAsync(equipmentIds, DateTime.UtcNow.Date, DateTime.UtcNow.Date, ReportFormat.Excel));
    }

    [Fact]
    public async Task ExportOeeReportAsync_ShouldPrefetchEquipmentMetadataOnce()
    {
        var equipmentService = new FakeEquipmentService(
        [
            new EquipmentDto(1, "EQ-001", "CNC Lathe 1", null, 1, "Line A", 120, 10m, true),
            new EquipmentDto(2, "EQ-002", "CNC Lathe 2", null, 1, "Line A", 120, 10m, true)
        ]);
        ReportExportService service = CreateService(equipmentService);

        await service.ExportOeeReportAsync([1, 2], DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1), ReportFormat.Excel);

        Assert.Equal(1, equipmentService.GetAllCallCount);
        Assert.Equal(0, equipmentService.GetByIdCallCount);
        Assert.Equal(4, equipmentService.CalculateOeeCallCount);
    }

    private static ReportExportService CreateService()
    {
        return new ReportExportService(
            new FakeWorkOrderService(),
            new FakeQualityService(),
            new FakeEquipmentService());
    }

    private static ReportExportService CreateService(FakeEquipmentService equipmentService)
    {
        return new ReportExportService(
            new FakeWorkOrderService(),
            new FakeQualityService(),
            equipmentService);
    }

    private sealed class FakeWorkOrderService : IWorkOrderService
    {
        public Task<IEnumerable<WorkOrderDto>> GetAllAsync()
        {
            IEnumerable<WorkOrderDto> orders =
            [
                new WorkOrderDto(
                    1,
                    "WO-001",
                    1,
                    "Rotor",
                    1,
                    "Line A",
                    100,
                    90,
                    88,
                    WorkOrderStatus.InProgress,
                    DateTime.UtcNow.Date.AddHours(8),
                    DateTime.UtcNow.Date.AddHours(16),
                    DateTime.UtcNow.Date.AddHours(8),
                    null,
                    0.9m),
                new WorkOrderDto(
                    2,
                    "WO-002",
                    1,
                    "Rotor",
                    1,
                    "Line A",
                    50,
                    50,
                    50,
                    WorkOrderStatus.Completed,
                    DateTime.UtcNow.Date.AddHours(9),
                    DateTime.UtcNow.Date.AddHours(14),
                    DateTime.UtcNow.Date.AddHours(9),
                    DateTime.UtcNow.Date.AddHours(13),
                    1m)
            ];

            return Task.FromResult(orders);
        }

        public Task<WorkOrderDto?> GetByIdAsync(int id) => Task.FromResult<WorkOrderDto?>(null);

        public Task<WorkOrderDto> CreateAsync(CreateWorkOrderRequest request) => throw new NotSupportedException();

        public Task<WorkOrderDto> StartAsync(int id) => throw new NotSupportedException();

        public Task<WorkOrderDto> ReportAsync(int id, ReportProductionRequest request) => throw new NotSupportedException();

        public Task<WorkOrderDto> CompleteAsync(int id) => throw new NotSupportedException();

        public Task<IEnumerable<WorkOrderDto>> GetTodayWorkOrdersAsync(int? productionLineId = null) => GetAllAsync();

        public Task<IEnumerable<WorkOrderDto>> GetDelayedWorkOrdersAsync(DateTime? startDate = null, DateTime? endDate = null) => Task.FromResult<IEnumerable<WorkOrderDto>>([]);
    }

    private sealed class FakeQualityService : IQualityService
    {
        public Task<IEnumerable<QualityInspectionDto>> GetInspectionsAsync()
        {
            IEnumerable<QualityInspectionDto> inspections =
            [
                new QualityInspectionDto(1, "QI-001", "B-001", 1, 1, "qa-1", "QA One", 10, 9, 1, InspectionStatus.Fail, DateTime.UtcNow.Date.AddHours(10), "Scratch"),
                new QualityInspectionDto(2, "QI-002", "B-002", 2, 1, "qa-1", "QA One", 10, 10, 0, InspectionStatus.Pass, DateTime.UtcNow.Date.AddHours(11), null)
            ];

            return Task.FromResult(inspections);
        }

        public Task<QualityInspectionDto> CreateInspectionAsync(CreateQualityInspectionRequest request) => throw new NotSupportedException();

        public Task<BatchTraceDto> TraceBatchAsync(string batchNumber) => throw new NotSupportedException();

        public Task<IEnumerable<DefectAnalysisDto>> AnalyzeDefectsAsync()
        {
            IEnumerable<DefectAnalysisDto> defects =
            [
                new DefectAnalysisDto(1, "D001", "Scratch", 3)
            ];

            return Task.FromResult(defects);
        }
    }

    private sealed class FakeEquipmentService : IEquipmentService
    {
        private readonly IReadOnlyList<EquipmentDto> _equipment;

        public FakeEquipmentService()
            : this([new EquipmentDto(1, "EQ-001", "CNC Lathe", null, 1, "Line A", 120, 10m, true)])
        {
        }

        public FakeEquipmentService(IReadOnlyList<EquipmentDto> equipment)
        {
            _equipment = equipment;
        }

        public int GetAllCallCount { get; private set; }

        public int GetByIdCallCount { get; private set; }

        public int CalculateOeeCallCount { get; private set; }

        public Task<IEnumerable<EquipmentDto>> GetAllAsync()
        {
            GetAllCallCount++;
            return Task.FromResult<IEnumerable<EquipmentDto>>(_equipment);
        }

        public Task<EquipmentDto?> GetByIdAsync(int id)
        {
            GetByIdCallCount++;
            return Task.FromResult(_equipment.FirstOrDefault(equipment => equipment.Id == id));
        }

        public Task<IEnumerable<EquipmentStatusDto>> GetStatusHistoryAsync(int equipmentId) => Task.FromResult<IEnumerable<EquipmentStatusDto>>([]);

        public Task<IEnumerable<EquipmentAlarmDto>> GetAlarmsAsync(int equipmentId) => Task.FromResult<IEnumerable<EquipmentAlarmDto>>([]);

        public Task<OeeDto> CalculateOeeAsync(int equipmentId, DateTime date)
        {
            CalculateOeeCallCount++;
            return Task.FromResult(new OeeDto(equipmentId, date.Date, 0.8m, 0.9m, 0.95m, 0.684m, 100, 95, 900));
        }
    }
}
