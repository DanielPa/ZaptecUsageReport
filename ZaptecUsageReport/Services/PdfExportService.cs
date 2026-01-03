using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZaptecUsageReport.Models;

namespace ZaptecUsageReport.Services;

public class PdfExportService
{
    private readonly double _costPerKwh;
    private readonly string _employee;
    private readonly string _address;
    private readonly string _vehicleLicensePlate;
    private readonly string _vehicleModel;

    public PdfExportService(double costPerKwh = 0.25, string employee = "", string address = "",
        string vehicleLicensePlate = "", string vehicleModel = "")
    {
        _costPerKwh = costPerKwh;
        _employee = employee;
        _address = address;
        _vehicleLicensePlate = vehicleLicensePlate;
        _vehicleModel = vehicleModel;
    }

    public string GeneratePdfReport(
        List<ChargeSession> sessions,
        string installationName,
        DateTime fromDate,
        DateTime toDate,
        string outputPath)
    {
        // Configure QuestPDF license (Community license for free use)
        QuestPDF.Settings.License = LicenseType.Community;

        // Calculate totals
        var totalEnergy = sessions.Sum(s => s.Energy);
        var totalCost = totalEnergy * _costPerKwh;
        var totalDuration = TimeSpan.FromHours(sessions.Sum(s => s.DeviceId != null ?
            (s.EndDateTime - s.StartDateTime).TotalHours : 0));
        var totalSessions = sessions.Count;

        // Generate PDF
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header()
                    .Element(container => ComposeHeader(container, installationName, fromDate, toDate,
                        totalSessions, totalEnergy, totalCost, totalDuration));

                page.Content()
                    .Element(container => ComposeContent(container, sessions));

                page.Footer()
                    .Element(ComposeFooter);
            });
        })
        .GeneratePdf(outputPath);

        return outputPath;
    }
    private void ComposeHeader(IContainer container, string installationName, DateTime fromDate,
        DateTime toDate, int totalSessions, double totalEnergy, double totalCost, TimeSpan totalDuration)
    {
        container.Column(column =>
        {
            // Title
            column.Item().Background(Colors.Blue.Darken2).Padding(10).Row(row =>
            {
                row.RelativeItem().AlignLeft().Text("Ladebericht")
                    .FontSize(18)
                    .Bold()
                    .FontColor(Colors.White);

                row.RelativeItem().AlignRight().Text($"{DateTime.Now:dd.MM.yyyy HH:mm}")
                    .FontSize(10)
                    .FontColor(Colors.White);
            });

            // Summary Information Section (German labels like Excel template)
            column.Item().PaddingTop(15).PaddingBottom(10).Table(table =>
            {
                // Define two columns for labels and values
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2f);   // Label column (left)
                    columns.RelativeColumn(3f);   // Value column (left)
                    columns.RelativeColumn(0.5f); // Spacer
                    columns.RelativeColumn(2.5f); // Label column (right)
                    columns.RelativeColumn(2f);   // Value column (right)
                });

                // Row 1: Mitarbeiter | Exportdatum
                table.Cell().Text("Mitarbeiter:").FontSize(10).Bold();
                table.Cell().Text(_employee ?? "").FontSize(10);
                table.Cell().Text(""); // Spacer
                table.Cell().Text("Exportdatum:").FontSize(10).Bold();
                table.Cell().Text($"{DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(10);

                // Row 2: Adresse | Abfrage-Zeitraum
                table.Cell().Text("Adresse:").FontSize(10).Bold();
                table.Cell().Text(_address ?? "").FontSize(10);
                table.Cell().Text(""); // Spacer
                table.Cell().Text("Abfrage-Zeitraum:").FontSize(10).Bold();
                table.Cell().Text($"{fromDate:dd.MM.yyyy} - {toDate:dd.MM.yyyy}").FontSize(10);

                // Row 3: Kennzeichen | Anzahl Sessions
                table.Cell().Text("Kennzeichen:").FontSize(10).Bold();
                table.Cell().Text(_vehicleLicensePlate ?? "").FontSize(10);
                table.Cell().Text(""); // Spacer
                table.Cell().Text("Anzahl Ladesitzungen:").FontSize(10).Bold();
                table.Cell().Text($"{totalSessions}").FontSize(10);

                // Row 4: Modell | Gesamtladeleistung (kWh)
                table.Cell().Text("Fahrzeug-Modell:").FontSize(10).Bold();
                table.Cell().Text(_vehicleModel ?? "").FontSize(10);
                table.Cell().Text(""); // Spacer
                table.Cell().Text("Gesamtladeleistung (kWh):").FontSize(10).Bold();
                table.Cell().Text($"{totalEnergy:F2}").FontSize(10);

                // Row 5: Empty | Strompreis pro kWh (brutto)
                table.Cell().Text("").FontSize(10);
                table.Cell().Text("").FontSize(10);
                table.Cell().Text(""); // Spacer
                table.Cell().Text("Strompreis pro kWh (brutto):").FontSize(10).Bold();
                table.Cell().Text($"{_costPerKwh:F3} €").FontSize(10);

                // Row 6: Empty | Gesamtkosten (brutto)
                table.Cell().Text("").FontSize(10);
                table.Cell().Text("").FontSize(10);
                table.Cell().Text(""); // Spacer
                table.Cell().Text("Gesamtkosten (brutto):").FontSize(10).Bold();
                table.Cell().Text($"{totalCost:F2} €").FontSize(10).Bold().FontColor(Colors.Green.Darken2);
            });

            // Separator line
            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private void ComposeContent(IContainer container, List<ChargeSession> sessions)
    {
        container.PaddingTop(10).Table(table =>
        {
            // Define columns
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2f);   // Session ID
                columns.RelativeColumn(1.5f); // Device ID
                columns.RelativeColumn(1.5f); // Start DateTime
                columns.RelativeColumn(1.5f); // End DateTime
                columns.RelativeColumn(0.8f); // Duration
                columns.RelativeColumn(0.8f); // Energy
                columns.RelativeColumn(0.8f); // Cost
            });

            // Table Header
            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Background(Colors.Grey.Lighten2).Text("Sitzungs ID").Bold();
                header.Cell().Element(CellStyle).Background(Colors.Grey.Lighten2).Text("Geräte ID").Bold();
                header.Cell().Element(CellStyle).Background(Colors.Grey.Lighten2).Text("Start").Bold();
                header.Cell().Element(CellStyle).Background(Colors.Grey.Lighten2).Text("Ende").Bold();
                header.Cell().Element(CellStyle).Background(Colors.Grey.Lighten2).Text("Dauer").Bold();
                header.Cell().Element(CellStyle).Background(Colors.Grey.Lighten2).Text("Energie").Bold();
                header.Cell().Element(CellStyle).Background(Colors.Grey.Lighten2).Text("Kosten").Bold();

                static IContainer CellStyle(IContainer container)
                {
                    return container
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten1)
                        .Padding(5);
                }
            });

            // Table Content
            int rowIndex = 0;
            foreach (var session in sessions)
            {
                var bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;
                var duration = session.EndDateTime - session.StartDateTime;

                // Session ID
                table.Cell().Element(c => CellStyle(c, bgColor))
                    .Text(session.Id);

                // Device ID
                table.Cell().Element(c => CellStyle(c, bgColor))
                    .Text(session.DeviceId);

                // Start DateTime
                table.Cell().Element(c => CellStyle(c, bgColor))
                    .Text(session.StartDateTime.ToString("yyyy-MM-dd HH:mm"));

                // End DateTime
                table.Cell().Element(c => CellStyle(c, bgColor))
                    .Text(session.EndDateTime.ToString("yyyy-MM-dd HH:mm"));

                // Duration (HH:MM format)
                table.Cell().Element(c => CellStyle(c, bgColor))
                    .Text($"{(int)duration.TotalHours:D2}:{duration.Minutes:D2} h");

                // Energy
                table.Cell().Element(c => CellStyle(c, bgColor))
                    .AlignRight()
                    .Text($"{session.Energy:F2} kWh");

                // Cost (calculated from energy * cost per kWh)
                var sessionCost = session.Energy * _costPerKwh;
                table.Cell().Element(c => CellStyle(c, bgColor))
                    .AlignRight()
                    .Text($"{sessionCost:F2} €");

                rowIndex++;
            }

            static IContainer CellStyle(IContainer container, string backgroundColor)
            {
                return container
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten1)
                    .Background(backgroundColor)
                    .Padding(5);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().AlignBottom().Row(row =>
        {
            row.RelativeItem().AlignLeft().Text("Generated by Zaptec Usage Report")
                .FontSize(8)
                .FontColor(Colors.Grey.Darken1);

            row.RelativeItem().AlignRight()
                .DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1))
                .Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
        });
    }
}