using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text.Json;
using ZaptecUsageReport.Models;
using ZaptecUsageReport.Services;

namespace ZaptecUsageReport;

public static class WebServer
{
    public static async Task RunAsync(string[] args, IConfiguration configuration)
    {
        var apiBaseUrl     = configuration["Zaptec:ApiBaseUrl"]!;
        var installationId = configuration["Zaptec:InstallationId"]!;
        var username       = configuration["Zaptec:Username"]!;
        var password       = configuration["Zaptec:Password"]!;

        var builder = WebApplication.CreateBuilder(args);
        var port = configuration["Web:Port"] ?? "5080";
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

        var app = builder.Build();

        app.UseDefaultFiles();
        app.UseStaticFiles();

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        app.MapGet("/api/sessions", async (string from, string to) =>
        {
            if (!DateTime.TryParse(from, out var fromDate) || !DateTime.TryParse(to, out var toDate))
                return Results.BadRequest("Invalid date format. Use YYYY-MM-DD.");

            var client = new ZaptecApiClient(apiBaseUrl);
            await client.AuthenticateAsync(username, password);
            var sessions = await client.GetChargeHistoryAsync(installationId, fromDate, toDate);
            return Results.Json(sessions);
        });

        app.MapPost("/api/export/excel", async (HttpRequest req) =>
        {
            var body = await JsonSerializer.DeserializeAsync<ExportRequest>(req.Body, jsonOptions);
            if (body == null) return Results.BadRequest("Invalid request body.");

            if (!DateTime.TryParse(body.From, out var fromDate) || !DateTime.TryParse(body.To, out var toDate))
                return Results.BadRequest("Invalid date format.");

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "template.xlsx");
            if (!File.Exists(templatePath))
                return Results.Problem($"Template file not found: {templatePath}", statusCode: 500);

            var tempPath = Path.ChangeExtension(Path.GetTempFileName(), ".xlsx");
            try
            {
                var installationName = body.Sessions.FirstOrDefault()?.InstallationName ?? "Unknown Installation";
                var service = new ExcelExportService();
                service.ExportToExcel(body.Sessions, templatePath, tempPath, fromDate, toDate, installationName);

                var bytes = await File.ReadAllBytesAsync(tempPath);
                var fileName = $"ZaptecReport_{fromDate:yyyy-MM}.xlsx";
                return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        });

        app.MapPost("/api/export/pdf", async (HttpRequest req) =>
        {
            var body = await JsonSerializer.DeserializeAsync<ExportRequest>(req.Body, jsonOptions);
            if (body == null) return Results.BadRequest("Invalid request body.");

            if (!DateTime.TryParse(body.From, out var fromDate) || !DateTime.TryParse(body.To, out var toDate))
                return Results.BadRequest("Invalid date format.");

            var costPerKwhStr = configuration["Pricing:CostPerKwh"] ?? "0.25";
            var costPerKwh    = double.Parse(costPerKwhStr.Replace(',', '.'), CultureInfo.InvariantCulture);
            var employee      = configuration["ReportInfo:Employee"] ?? "";
            var address       = configuration["ReportInfo:Address"] ?? "";
            var licensePlate  = configuration["ReportInfo:VehicleLicensePlate"] ?? "";
            var model         = configuration["ReportInfo:VehicleModel"] ?? "";

            var tempPath = Path.ChangeExtension(Path.GetTempFileName(), ".pdf");
            try
            {
                var installationName = body.Sessions.FirstOrDefault()?.InstallationName ?? "Unknown Installation";
                var service = new PdfExportService(costPerKwh, employee, address, licensePlate, model);
                service.GeneratePdfReport(body.Sessions, installationName, fromDate, toDate, tempPath);

                var bytes = await File.ReadAllBytesAsync(tempPath);
                var fileName = $"ZaptecReport_{fromDate:yyyy-MM}.pdf";
                return Results.File(bytes, "application/pdf", fileName);
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        });

        Console.WriteLine($"Web UI running at http://localhost:{port}");
        await app.RunAsync();
    }
}

record ExportRequest(string From, string To, List<ChargeSession> Sessions);
