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
        QuestPDF.Settings.License = LicenseType.Community;

        var totalEnergy = sessions.Sum(s => s.Energy);
        var totalCost = totalEnergy * _costPerKwh;
        var totalDuration = TimeSpan.FromHours(sessions.Sum(s => s.DeviceId != null ?
            (s.EndDateTime - s.StartDateTime).TotalHours : 0));
        var totalSessions = sessions.Count;

        var signedSessions = sessions
            .Where(s => !string.IsNullOrEmpty(s.SignedSession))
            .ToList();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header()
                    .Element(c => ComposeHeader(c, installationName, fromDate, toDate,
                        totalSessions, totalEnergy, totalCost, totalDuration));

                page.Content()
                    .Element(c => ComposeContent(c, sessions));

                page.Footer().Element(ComposeFooter);
            });

            if (signedSessions.Count > 0)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Content()
                        .Element(c => ComposeSignatureAppendix(c, signedSessions));

                    page.Footer().Element(ComposeFooter);
                });
            }
        })
        .GeneratePdf(outputPath);

        return outputPath;
    }

    // Inserts newlines every lineWidth characters so QuestPDF can wrap long strings without spaces
    private static string ChunkText(string text, int lineWidth)
    {
        if (text.Length <= lineWidth) return text;
        return string.Join("\n",
            Enumerable.Range(0, (text.Length + lineWidth - 1) / lineWidth)
                .Select(i => text.Substring(i * lineWidth, Math.Min(lineWidth, text.Length - i * lineWidth))));
    }

    private void ComposeHeader(IContainer container, string installationName, DateTime fromDate,
        DateTime toDate, int totalSessions, double totalEnergy, double totalCost, TimeSpan totalDuration)
    {
        container.Column(column =>
        {
            column.Item().Background(Colors.Blue.Darken2).Padding(10).Row(row =>
            {
                row.RelativeItem().AlignLeft().Text("Ladebericht")
                    .FontSize(18).Bold().FontColor(Colors.White);
                row.RelativeItem().AlignRight().Text($"{DateTime.Now:dd.MM.yyyy HH:mm}")
                    .FontSize(10).FontColor(Colors.White);
            });

            column.Item().PaddingTop(15).PaddingBottom(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(3f);
                    columns.RelativeColumn(0.5f);
                    columns.RelativeColumn(2.5f);
                    columns.RelativeColumn(2f);
                });

                table.Cell().Text("Mitarbeiter:").FontSize(10).Bold();
                table.Cell().Text(_employee ?? "").FontSize(10);
                table.Cell().Text("");
                table.Cell().Text("Exportdatum:").FontSize(10).Bold();
                table.Cell().Text($"{DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(10);

                table.Cell().Text("Adresse:").FontSize(10).Bold();
                table.Cell().Text(_address ?? "").FontSize(10);
                table.Cell().Text("");
                table.Cell().Text("Abfrage-Zeitraum:").FontSize(10).Bold();
                table.Cell().Text($"{fromDate:dd.MM.yyyy} - {toDate:dd.MM.yyyy}").FontSize(10);

                table.Cell().Text("Kennzeichen:").FontSize(10).Bold();
                table.Cell().Text(_vehicleLicensePlate ?? "").FontSize(10);
                table.Cell().Text("");
                table.Cell().Text("Anzahl Ladesitzungen:").FontSize(10).Bold();
                table.Cell().Text($"{totalSessions}").FontSize(10);

                table.Cell().Text("Fahrzeug-Modell:").FontSize(10).Bold();
                table.Cell().Text(_vehicleModel ?? "").FontSize(10);
                table.Cell().Text("");
                table.Cell().Text("Gesamtladeleistung (kWh):").FontSize(10).Bold();
                table.Cell().Text($"{totalEnergy:F2}").FontSize(10);

                table.Cell().Text("").FontSize(10);
                table.Cell().Text("").FontSize(10);
                table.Cell().Text("");
                table.Cell().Text("Strompreis pro kWh (brutto):").FontSize(10).Bold();
                table.Cell().Text($"{_costPerKwh:F3} €").FontSize(10);

                table.Cell().Text("").FontSize(10);
                table.Cell().Text("").FontSize(10);
                table.Cell().Text("");
                table.Cell().Text("Gesamtkosten (brutto):").FontSize(10).Bold();
                table.Cell().Text($"{totalCost:F2} €").FontSize(10).Bold().FontColor(Colors.Green.Darken2);
            });

            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private void ComposeContent(IContainer container, List<ChargeSession> sessions)
    {
        container.PaddingTop(10).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2f);   // Session ID
                columns.RelativeColumn(1.5f); // Device ID
                columns.RelativeColumn(1.5f); // Start
                columns.RelativeColumn(1.5f); // End
                columns.RelativeColumn(0.8f); // Duration
                columns.RelativeColumn(0.8f); // Energy
                columns.RelativeColumn(0.8f); // Cost
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCellStyle).Text("Sitzungs ID").Bold();
                header.Cell().Element(HeaderCellStyle).Text("Geräte ID").Bold();
                header.Cell().Element(HeaderCellStyle).Text("Start").Bold();
                header.Cell().Element(HeaderCellStyle).Text("Ende").Bold();
                header.Cell().Element(HeaderCellStyle).Text("Dauer").Bold();
                header.Cell().Element(HeaderCellStyle).Text("Energie").Bold();
                header.Cell().Element(HeaderCellStyle).Text("Kosten").Bold();

                static IContainer HeaderCellStyle(IContainer c) =>
                    c.Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten2).Padding(5);
            });

            var rowIndex = 0;
            foreach (var session in sessions)
            {
                var bg = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;
                var duration = session.EndDateTime - session.StartDateTime;
                var sessionCost = session.Energy * _costPerKwh;

                table.Cell().Element(c => Cell(c, bg)).Text(session.Id);
                table.Cell().Element(c => Cell(c, bg)).Text(session.DeviceId);
                table.Cell().Element(c => Cell(c, bg)).Text(session.StartDateTime.ToString("yyyy-MM-dd HH:mm"));
                table.Cell().Element(c => Cell(c, bg)).Text(session.EndDateTime.ToString("yyyy-MM-dd HH:mm"));
                table.Cell().Element(c => Cell(c, bg)).Text($"{(int)duration.TotalHours:D2}:{duration.Minutes:D2} h");
                table.Cell().Element(c => Cell(c, bg)).AlignRight().Text($"{session.Energy:F2} kWh");
                table.Cell().Element(c => Cell(c, bg)).AlignRight().Text($"{sessionCost:F2} €");

                rowIndex++;
            }

            static IContainer Cell(IContainer c, string bg) =>
                c.Border(1).BorderColor(Colors.Grey.Lighten1).Background(bg).Padding(5);
        });
    }

    private static void ComposeSignatureAppendix(IContainer container, List<ChargeSession> sessions)
    {
        container.Column(column =>
        {
            column.Item().Background(Colors.Blue.Darken2).Padding(10)
                .Text("Anhang: OCMF-Signaturen")
                .FontSize(14).Bold().FontColor(Colors.White);

            column.Item().PaddingTop(6).PaddingBottom(2)
                .Text("Die Signaturen können mit einem OCMF-kompatiblen Prüfwerkzeug (z.B. transparenz.software) verifiziert werden.")
                .FontSize(8).FontColor(Colors.Grey.Darken1).Italic();

            column.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

            foreach (var session in sessions)
            {
                if (string.IsNullOrEmpty(session.SignedSession)) continue;

                column.Item().Column(entry =>
                {
                    entry.Item()
                        .Text($"{session.StartDateTime:dd.MM.yyyy HH:mm} Uhr  –  {session.Energy:F2} kWh")
                        .Bold().FontSize(9).FontColor(Colors.Blue.Darken2);

                    entry.Item().PaddingTop(3).PaddingBottom(8)
                        .Text(ChunkText(session.SignedSession, 130))
                        .FontSize(6.5f);

                    entry.Item().PaddingBottom(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                });
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().AlignBottom().Row(row =>
        {
            row.RelativeItem().AlignLeft().Text("Generated by Zaptec Usage Report")
                .FontSize(8).FontColor(Colors.Grey.Darken1);

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
