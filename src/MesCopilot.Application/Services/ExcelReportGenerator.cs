using ClosedXML.Excel;
using MesCopilot.Application.Dtos.Reports;

namespace MesCopilot.Application.Services;

public static class ExcelReportGenerator
{
    public static byte[] GenerateProductionReport(ProductionReportDto report)
    {
        using XLWorkbook workbook = new();
        IXLWorksheet worksheet = workbook.Worksheets.Add("Production");
        WriteTitle(worksheet, "MES Copilot - Production Daily Report", 6);
        worksheet.Cell(2, 1).Value = $"Date: {report.ReportDate:yyyy-MM-dd}";
        worksheet.Cell(3, 1).Value = $"Total: {report.TotalOrders} | Completed: {report.CompletedOrders} | Delayed: {report.DelayedOrders} | Completion: {report.CompletionRate:P1}";

        string[] headers = ["Work Order", "Product", "Planned", "Actual", "Status", "Due Date"];
        WriteHeaders(worksheet, 5, headers);

        int row = 6;
        foreach (ProductionReportLineItemDto item in report.LineItems)
        {
            worksheet.Cell(row, 1).Value = item.WorkOrderCode;
            worksheet.Cell(row, 2).Value = item.ProductName;
            worksheet.Cell(row, 3).Value = item.PlannedQuantity;
            worksheet.Cell(row, 4).Value = item.ActualQuantity;
            worksheet.Cell(row, 5).Value = item.Status.ToString();
            worksheet.Cell(row, 6).Value = item.DueDate.ToString("yyyy-MM-dd");
            row++;
        }

        return Save(workbook, worksheet);
    }

    public static byte[] GenerateQualityReport(QualityReportDto report)
    {
        using XLWorkbook workbook = new();
        IXLWorksheet worksheet = workbook.Worksheets.Add("Quality");
        WriteTitle(worksheet, "MES Copilot - Quality Report", 7);
        worksheet.Cell(2, 1).Value = $"Period: {report.FromDate:yyyy-MM-dd} ~ {report.ToDate:yyyy-MM-dd}";
        worksheet.Cell(3, 1).Value = $"Total: {report.TotalInspections} | Passed: {report.PassedInspections} | Failed: {report.FailedInspections} | Pass Rate: {report.PassRate:P1}";

        string[] headers = ["Inspection", "Batch", "Work Order", "Inspector", "Date", "Status", "Defects"];
        WriteHeaders(worksheet, 5, headers);

        int row = 6;
        foreach (QualityReportLineItemDto item in report.LineItems)
        {
            worksheet.Cell(row, 1).Value = item.InspectionCode;
            worksheet.Cell(row, 2).Value = item.BatchNumber;
            worksheet.Cell(row, 3).Value = item.WorkOrderId;
            worksheet.Cell(row, 4).Value = item.InspectorName;
            worksheet.Cell(row, 5).Value = item.InspectionDate.ToString("yyyy-MM-dd HH:mm");
            worksheet.Cell(row, 6).Value = item.Status.ToString();
            worksheet.Cell(row, 7).Value = item.DefectCount;
            row++;
        }

        return Save(workbook, worksheet);
    }

    public static byte[] GenerateOeeReport(OeeReportDto report)
    {
        using XLWorkbook workbook = new();
        IXLWorksheet worksheet = workbook.Worksheets.Add("OEE");
        WriteTitle(worksheet, "MES Copilot - OEE Report", 8);
        worksheet.Cell(2, 1).Value = $"Period: {report.FromDate:yyyy-MM-dd} ~ {report.ToDate:yyyy-MM-dd}";
        worksheet.Cell(3, 1).Value = $"Average OEE: {report.AverageOee:P1} | A: {report.AverageAvailability:P1} | P: {report.AveragePerformance:P1} | Q: {report.AverageQuality:P1}";

        string[] headers = ["Equipment", "Date", "Availability", "Performance", "Quality", "OEE", "Output", "Runtime"];
        WriteHeaders(worksheet, 5, headers);

        int row = 6;
        foreach (OeeReportLineItemDto item in report.LineItems)
        {
            worksheet.Cell(row, 1).Value = item.EquipmentName;
            worksheet.Cell(row, 2).Value = item.Date.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 3).Value = item.Availability;
            worksheet.Cell(row, 4).Value = item.Performance;
            worksheet.Cell(row, 5).Value = item.Quality;
            worksheet.Cell(row, 6).Value = item.Oee;
            worksheet.Cell(row, 7).Value = item.TotalOutput;
            worksheet.Cell(row, 8).Value = item.RunningMinutes;
            row++;
        }

        return Save(workbook, worksheet);
    }

    private static void WriteTitle(IXLWorksheet worksheet, string title, int columns)
    {
        worksheet.Cell(1, 1).Value = title;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, columns).Merge();
    }

    private static void WriteHeaders(IXLWorksheet worksheet, int row, IReadOnlyList<string> headers)
    {
        for (int index = 0; index < headers.Count; index++)
        {
            IXLCell cell = worksheet.Cell(row, index + 1);
            cell.Value = headers[index];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }
    }

    private static byte[] Save(XLWorkbook workbook, IXLWorksheet worksheet)
    {
        worksheet.Columns().AdjustToContents();
        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
