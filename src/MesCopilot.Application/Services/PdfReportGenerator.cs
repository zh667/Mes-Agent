using MesCopilot.Application.Dtos.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MesCopilot.Application.Services;

public static class PdfReportGenerator
{
    static PdfReportGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] GenerateProductionReport(ProductionReportDto report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page, "Production Daily Report", report.ReportDate.ToString("yyyy-MM-dd"));
                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Text($"Total Orders: {report.TotalOrders} | Completed: {report.CompletedOrders} | Delayed: {report.DelayedOrders} | Completion: {report.CompletionRate:P1}");
                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn(1.5f);
                        });
                        WriteHeader(table, ["Work Order", "Product", "Planned", "Actual", "Status", "Due"]);
                        foreach (ProductionReportLineItemDto item in report.LineItems.Take(50))
                        {
                            table.Cell().Element(Cell).Text(item.WorkOrderCode);
                            table.Cell().Element(Cell).Text(item.ProductName);
                            table.Cell().Element(Cell).Text(item.PlannedQuantity.ToString());
                            table.Cell().Element(Cell).Text(item.ActualQuantity.ToString());
                            table.Cell().Element(Cell).Text(item.Status.ToString());
                            table.Cell().Element(Cell).Text(item.DueDate.ToString("yyyy-MM-dd"));
                        }
                    });
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerateQualityReport(QualityReportDto report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page, "Quality Report", $"{report.FromDate:yyyy-MM-dd} ~ {report.ToDate:yyyy-MM-dd}");
                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Text($"Total Inspections: {report.TotalInspections} | Passed: {report.PassedInspections} | Failed: {report.FailedInspections} | Pass Rate: {report.PassRate:P1}");
                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });
                        WriteHeader(table, ["Inspection", "Batch", "Work Order", "Inspector", "Status", "Defects"]);
                        foreach (QualityReportLineItemDto item in report.LineItems.Take(50))
                        {
                            table.Cell().Element(Cell).Text(item.InspectionCode);
                            table.Cell().Element(Cell).Text(item.BatchNumber);
                            table.Cell().Element(Cell).Text(item.WorkOrderId.ToString());
                            table.Cell().Element(Cell).Text(item.InspectorName);
                            table.Cell().Element(Cell).Text(item.Status.ToString());
                            table.Cell().Element(Cell).Text(item.DefectCount.ToString());
                        }
                    });
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerateOeeReport(OeeReportDto report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page, "OEE Report", $"{report.FromDate:yyyy-MM-dd} ~ {report.ToDate:yyyy-MM-dd}", landscape: true);
                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Text($"Average OEE: {report.AverageOee:P1} | A: {report.AverageAvailability:P1} | P: {report.AveragePerformance:P1} | Q: {report.AverageQuality:P1}");
                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });
                        WriteHeader(table, ["Equipment", "Date", "A", "P", "Q", "OEE", "Output", "Runtime"]);
                        foreach (OeeReportLineItemDto item in report.LineItems.Take(50))
                        {
                            table.Cell().Element(Cell).Text(item.EquipmentName);
                            table.Cell().Element(Cell).Text(item.Date.ToString("yyyy-MM-dd"));
                            table.Cell().Element(Cell).Text($"{item.Availability:P0}");
                            table.Cell().Element(Cell).Text($"{item.Performance:P0}");
                            table.Cell().Element(Cell).Text($"{item.Quality:P0}");
                            table.Cell().Element(Cell).Text($"{item.Oee:P0}");
                            table.Cell().Element(Cell).Text(item.TotalOutput.ToString());
                            table.Cell().Element(Cell).Text(item.RunningMinutes.ToString());
                        }
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void ConfigurePage(PageDescriptor page, string title, string period, bool landscape = false)
    {
        page.Size(landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
        page.Margin(2, Unit.Centimetre);
        page.DefaultTextStyle(style => style.FontSize(9));
        page.Header().Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("MES Copilot").FontSize(16).Bold().FontColor(Colors.Teal.Darken2);
                column.Item().Text(title).FontSize(12).SemiBold();
                column.Item().Text($"Period: {period}").FontSize(9).FontColor(Colors.Grey.Darken1);
            });
            row.ConstantItem(120).AlignRight().Text(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")).FontSize(8);
        });
        page.Footer().AlignCenter().Text(text =>
        {
            text.Span("Page ");
            text.CurrentPageNumber();
            text.Span(" / ");
            text.TotalPages();
        });
    }

    private static void WriteHeader(TableDescriptor table, IReadOnlyList<string> headers)
    {
        table.Header(header =>
        {
            foreach (string label in headers)
            {
                header.Cell().Element(HeaderCell).Text(label);
            }
        });
    }

    private static IContainer HeaderCell(IContainer container)
    {
        return container.DefaultTextStyle(style => style.SemiBold())
            .PaddingVertical(5)
            .BorderBottom(1)
            .BorderColor(Colors.Black);
    }

    private static IContainer Cell(IContainer container)
    {
        return container.PaddingVertical(3)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2);
    }
}
