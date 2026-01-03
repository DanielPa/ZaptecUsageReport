using ClosedXML.Excel;
using ZaptecUsageReport.Models;

namespace ZaptecUsageReport.Services;

public class ExcelExportService
{
    public void ExportToExcel(List<ChargeSession> sessions, string templatePath, string outputPath, DateTime? fromDate = null, DateTime? toDate = null, string? installationName = null)
    {
        // Load the template workbook
        using var workbook = new XLWorkbook(templatePath);
        var worksheet = workbook.Worksheet(1); // Use first worksheet

        // Populate dynamic header data
        PopulateHeaderData(worksheet, sessions, fromDate, toDate, installationName);

        // Check if there's an Excel Table in the worksheet
        var table = worksheet.Tables.FirstOrDefault();
        int startRow;
        int startColumn = 1;

        // If working with a table, save template row data before clearing
        List<(bool hasFormula, string formula, XLCellValue value, IXLStyle style)>? templateData = null;

        if (table != null)
        {
            // If there's a table, insert rows starting from the first data row
            startRow = table.HeadersRow().RowNumber() + 1;
            startColumn = table.HeadersRow().FirstCell().Address.ColumnNumber;

            // Save the first data row as template BEFORE clearing (contains formulas/formatting)
            if (table.DataRange.RowCount() > 0)
            {
                var templateRow = table.DataRange.FirstRow();

                // Create a copy of the template row data
                templateData = new List<(bool hasFormula, string formula, XLCellValue value, IXLStyle style)>();
                for (int col = startColumn; col <= startColumn + table.ColumnCount() - 1; col++)
                {
                    var cell = templateRow.Cell(col - startColumn + 1);
                    templateData.Add((cell.HasFormula, cell.FormulaA1, cell.Value, cell.Style));
                }

                // Clear existing data rows
                table.DataRange.Delete(XLShiftDeletedCells.ShiftCellsUp);
            }
        }
        else
        {
            // No table found, assume headers in row 1
            startRow = 2;
        }

        // Populate the data
        int currentRow = startRow;
        bool isFirstRow = true;
        foreach (var session in sessions.OrderBy(s => s.StartDateTime))
        {
            var duration = session.EndDateTime - session.StartDateTime;

            // Insert a new row if we're working with a table and not the first row
            if (table != null && !isFirstRow)
            {
                worksheet.Row(currentRow).InsertRowsAbove(1);
            }

            // For the first row OR newly inserted rows, apply template data to additional columns
            if (table != null && templateData != null)
            {
                // Copy template data to additional columns (beyond the 9 standard data columns)
                for (int i = 6; i < templateData.Count; i++)
                {
                    var col = startColumn + i;
                    var targetCell = worksheet.Cell(currentRow, col);
                    var (hasFormula, formula, value, style) = templateData[i];

                    // Copy formula or value
                    if (hasFormula)
                    {
                        targetCell.FormulaA1 = formula;
                    }
                    else if (!value.IsBlank)
                    {
                        targetCell.Value = value;
                    }

                    // Copy style
                    targetCell.Style = style;
                }
            }

            // Populate the standard data columns
            worksheet.Cell(currentRow, startColumn + 0).Value = session.Id;
            worksheet.Cell(currentRow, startColumn + 1).Value = session.DeviceId;
            worksheet.Cell(currentRow, startColumn + 2).Value = session.StartDateTime;
            worksheet.Cell(currentRow, startColumn + 3).Value = session.EndDateTime;
            worksheet.Cell(currentRow, startColumn + 4).Value = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}";
            worksheet.Cell(currentRow, startColumn + 5).Value = session.Energy;
            worksheet.Cell(currentRow, startColumn + 6).Value = session.SignedSession;

            currentRow++;
            isFirstRow = false;
        }

        // If there's a table, resize it to include all the new data
        if (table != null)
        {
            var lastRow = startRow + sessions.Count - 1;
            var lastColumn = startColumn + table.ColumnCount() -1; 
            var newRange = worksheet.Range(
                table.HeadersRow().RowNumber(),
                startColumn,
                lastRow,
                lastColumn
            );
            table.Resize(newRange);
        }

        // Recalculate all formulas
        workbook.RecalculateAllFormulas();

        // Save the workbook
        workbook.SaveAs(outputPath);
    }

    private void PopulateHeaderData(IXLWorksheet worksheet, List<ChargeSession> sessions, DateTime? fromDate, DateTime? toDate, string? installationName)
    {
        // Try to find and populate named cells or cells with placeholder text
        var cellsToCheck = worksheet.CellsUsed();

        foreach (var cell in cellsToCheck)
        {
            var cellValue = cell.GetString();

            // Replace placeholders with actual data
            switch (cellValue.ToUpper())
            {
                case "{{EXPORT_DATE}}" or "{{EXPORTDATE}}":
                    cell.Value = DateTime.Now;
                    cell.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                    break;

                case "{{FROM_DATE}}" or "{{FROMDATE}}":
                    if (fromDate.HasValue)
                    {
                        cell.Value = fromDate.Value;
                        cell.Style.DateFormat.Format = "dd.MM.yyyy";
                    }
                    break;

                case "{{TO_DATE}}" or "{{TODATE}}":
                    if (toDate.HasValue)
                    {
                        cell.Value = toDate.Value;
                        cell.Style.DateFormat.Format = "dd.MM.yyyy";
                    }
                    break;

                case "{{DATE_RANGE}}" or "{{DATERANGE}}":
                    if (fromDate.HasValue && toDate.HasValue)
                    {
                        cell.Value = $"{fromDate.Value:dd.MM.yyyy} bis {toDate.Value:dd.MM.yyyy}";
                    }
                    break;

                case "{{INSTALLATION_NAME}}" or "{{INSTALLATIONNAME}}":
                    if (!string.IsNullOrEmpty(installationName))
                    {
                        cell.Value = installationName;
                    }
                    break;

                case "{{SESSION_COUNT}}" or "{{SESSIONCOUNT}}":
                    cell.Value = sessions.Count;
                    break;

                case "{{TOTAL_ENERGY}}" or "{{TOTALENERGY}}":
                    cell.Value = sessions.Sum(s => s.Energy);
                    cell.Style.NumberFormat.Format = "0.00";
                    break;

                case "{{TOTAL_DURATION}}" or "{{TOTALDURATION}}":
                    var totalHours = sessions.Sum(s => (s.EndDateTime - s.StartDateTime).TotalHours);
                    cell.Value = totalHours;
                    cell.Style.NumberFormat.Format = "0.00";
                    break;

                default:
                    // Check if cell value contains any placeholder
                    if (cellValue.Contains("{{") && cellValue.Contains("}}"))
                    {
                        var updatedValue = cellValue;

                        if (fromDate.HasValue)
                            updatedValue = updatedValue.Replace("{{FROM_DATE}}", fromDate.Value.ToString("yyyy-MM-dd"))
                                                       .Replace("{{FROMDATE}}", fromDate.Value.ToString("yyyy-MM-dd"));

                        if (toDate.HasValue)
                            updatedValue = updatedValue.Replace("{{TO_DATE}}", toDate.Value.ToString("yyyy-MM-dd"))
                                                       .Replace("{{TODATE}}", toDate.Value.ToString("yyyy-MM-dd"));

                        updatedValue = updatedValue.Replace("{{EXPORT_DATE}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                                                   .Replace("{{EXPORTDATE}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                                                   .Replace("{{SESSION_COUNT}}", sessions.Count.ToString())
                                                   .Replace("{{SESSIONCOUNT}}", sessions.Count.ToString())
                                                   .Replace("{{TOTAL_ENERGY}}", sessions.Sum(s => s.Energy).ToString("F2"))
                                                   .Replace("{{TOTALENERGY}}", sessions.Sum(s => s.Energy).ToString("F2"));

                        if (!string.IsNullOrEmpty(installationName))
                            updatedValue = updatedValue.Replace("{{INSTALLATION_NAME}}", installationName)
                                                       .Replace("{{INSTALLATIONNAME}}", installationName);

                        if (updatedValue != cellValue)
                        {
                            cell.Value = updatedValue;
                        }
                    }
                    break;
            }
        }

        // Also check for named ranges
        foreach (var namedRange in worksheet.Workbook.NamedRanges)
        {
            var cell = namedRange.Ranges.FirstOrDefault()?.FirstCell();
            if (cell == null) continue;

            switch (namedRange.Name.ToUpper())
            {
                case "EXPORT_DATE" or "EXPORTDATE":
                    cell.Value = DateTime.Now;
                    cell.Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                    break;

                case "FROM_DATE" or "FROMDATE":
                    if (fromDate.HasValue)
                    {
                        cell.Value = fromDate.Value;
                        cell.Style.DateFormat.Format = "yyyy-MM-dd";
                    }
                    break;

                case "TO_DATE" or "TODATE":
                    if (toDate.HasValue)
                    {
                        cell.Value = toDate.Value;
                        cell.Style.DateFormat.Format = "yyyy-MM-dd";
                    }
                    break;

                case "DATE_RANGE" or "DATERANGE":
                    if (fromDate.HasValue && toDate.HasValue)
                    {
                        cell.Value = $"{fromDate.Value:yyyy-MM-dd} to {toDate.Value:yyyy-MM-dd}";
                    }
                    break;

                case "INSTALLATION_NAME" or "INSTALLATIONNAME":
                    if (!string.IsNullOrEmpty(installationName))
                    {
                        cell.Value = installationName;
                    }
                    break;

                case "SESSION_COUNT" or "SESSIONCOUNT":
                    cell.Value = sessions.Count;
                    break;

                case "TOTAL_ENERGY" or "TOTALENERGY":
                    cell.Value = sessions.Sum(s => s.Energy);
                    cell.Style.NumberFormat.Format = "0.00";
                    break;

                case "TOTAL_DURATION" or "TOTALDURATION":
                    var totalHours = sessions.Sum(s => (s.EndDateTime - s.StartDateTime).TotalHours);
                    cell.Value = totalHours;
                    cell.Style.NumberFormat.Format = "0.00";
                    break;
            }
        }
    }
}