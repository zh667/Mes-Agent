using ClosedXML.Excel;
using MesCopilot.Application.Dtos.Auditing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MesCopilot.Application.Services;

public static class AuditReportGenerator
{
    static AuditReportGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(IReadOnlyList<AuditReportRowDto> rows, ReportFormat format)
    {
        return format == ReportFormat.Excel ? GenerateExcel(rows) : GeneratePdf(rows);
    }

    private static byte[] GenerateExcel(IReadOnlyList<AuditReportRowDto> rows)
    {
        using XLWorkbook workbook = new();
        IXLWorksheet sheet = workbook.Worksheets.Add("Audit");
        string[] headers = ["Timestamp", "Category", "Actor", "Action", "Target", "Status", "Duration ms"];
        for (int column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
            sheet.Cell(1, column + 1).Style.Font.Bold = true;
        }

        for (int index = 0; index < rows.Count; index++)
        {
            AuditReportRowDto row = rows[index];
            int targetRow = index + 2;
            sheet.Cell(targetRow, 1).Value = row.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            sheet.Cell(targetRow, 2).Value = row.Category;
            sheet.Cell(targetRow, 3).Value = row.Actor;
            sheet.Cell(targetRow, 4).Value = row.Action;
            sheet.Cell(targetRow, 5).Value = row.Target;
            sheet.Cell(targetRow, 6).Value = row.Status;
            sheet.Cell(targetRow, 7).Value = row.DurationMilliseconds;
        }

        sheet.Columns().AdjustToContents(1, 60);
        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] GeneratePdf(IReadOnlyList<AuditReportRowDto> rows)
    {
        const int maxDisplayedRows = 50;
        IReadOnlyList<AuditReportRowDto> displayed = rows.Take(maxDisplayedRows).ToList();
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.Header().Text("MES Copilot Audit Report").FontSize(18).SemiBold();
                page.Content().PaddingVertical(12).Column(column =>
                {
                    column.Item().Text($"Showing {displayed.Count} of {rows.Count} records").FontSize(9).FontColor(Colors.Grey.Darken1);
                    column.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(2.5f);
                            columns.RelativeColumn();
                        });
                        foreach (string header in new[] { "Timestamp", "Category", "Actor", "Action", "Target", "Status" })
                        {
                            table.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(header).SemiBold();
                        }

                        foreach (AuditReportRowDto row in displayed)
                        {
                            table.Cell().Padding(4).Text(row.Timestamp.ToString("yyyy-MM-dd HH:mm"));
                            table.Cell().Padding(4).Text(row.Category);
                            table.Cell().Padding(4).Text(row.Actor);
                            table.Cell().Padding(4).Text(row.Action);
                            table.Cell().Padding(4).Text(row.Target);
                            table.Cell().Padding(4).Text(row.Status);
                        }
                    });
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                });
            });
        }).GeneratePdf();
    }
}
