using ClosedXML.Excel;
using ZaptecUsageReport.Models;

namespace ZaptecUsageReport.Services;

public class ExcelExportService
{
    public void ExportToExcel(List<ChargeSession> sessions, string templatePath, string outputPath)
    {
        // Load the template workbook
        using var workbook = new XLWorkbook(templatePath);
        var worksheet = workbook.Worksheet(1); // Use first worksheet

        // Find the data start row (assumes template has headers in row 1)
        int startRow = 2;

        // Populate the data
        int currentRow = startRow;
        foreach (var session in sessions.OrderBy(s => s.StartDateTime))
        {
            var duration = session.EndDateTime - session.StartDateTime;

            worksheet.Cell(currentRow, 1).Value = session.StartDateTime;
            worksheet.Cell(currentRow, 2).Value = session.EndDateTime;
            worksheet.Cell(currentRow, 3).Value = duration.TotalHours;
            worksheet.Cell(currentRow, 4).Value = session.Energy;
            worksheet.Cell(currentRow, 5).Value = session.UserFullName;
            worksheet.Cell(currentRow, 6).Value = session.UserEmail;
            worksheet.Cell(currentRow, 7).Value = session.ChargerName;
            worksheet.Cell(currentRow, 8).Value = session.DeviceName;
            worksheet.Cell(currentRow, 9).Value = session.Id;

            currentRow++;
        }

        // Recalculate all formulas
        workbook.RecalculateAllFormulas();

        // Save the workbook
        workbook.SaveAs(outputPath);
    }
}