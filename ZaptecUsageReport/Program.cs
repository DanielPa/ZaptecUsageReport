using Microsoft.Extensions.Configuration;
using ZaptecUsageReport.Services;

// Load configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .Build();

// Get configuration values
var apiBaseUrl = configuration["Zaptec:ApiBaseUrl"] ?? throw new Exception("ApiBaseUrl not configured");
var installationId = configuration["Zaptec:InstallationId"] ?? throw new Exception("InstallationId not configured");
var username = configuration["Zaptec:Username"] ?? throw new Exception("Username not configured. Set with: dotnet user-secrets set \"Zaptec:Username\" \"your_email@example.com\"");
var password = configuration["Zaptec:Password"] ?? throw new Exception("Password not configured. Set with: dotnet user-secrets set \"Zaptec:Password\" \"your_password\"");

try
{
    Console.WriteLine("Zaptec Usage Report Generator");
    Console.WriteLine("==============================\n");

    // Initialize API client
    var apiClient = new ZaptecApiClient(apiBaseUrl);

    // Authenticate
    Console.WriteLine("Authenticating...");
    await apiClient.AuthenticateAsync(username, password);
    Console.WriteLine("Authentication successful!\n");

    // Get month selection from user
    DateTime fromDate, toDate;

    Console.WriteLine("Select the month for the report:");
    Console.WriteLine("1. Current month (so far)");
    Console.WriteLine("2. Last month (complete)");
    Console.WriteLine("3. Specific month (enter year and month)");
    Console.Write("\nEnter your choice (1-3): ");

    var choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            // Current month from first day to today
            var now = DateTime.Now;
            fromDate = new DateTime(now.Year, now.Month, 1);
            toDate = now;
            break;

        case "2":
            // Last complete month
            var lastMonth = DateTime.Now.AddMonths(-1);
            fromDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            toDate = fromDate.AddMonths(1).AddDays(-1);
            break;

        case "3":
            // Specific month
            Console.Write("Enter year (e.g., 2025): ");
            if (!int.TryParse(Console.ReadLine(), out var year))
            {
                Console.WriteLine("Invalid year. Exiting.");
                return;
            }

            Console.Write("Enter month (1-12): ");
            if (!int.TryParse(Console.ReadLine(), out var month) || month < 1 || month > 12)
            {
                Console.WriteLine("Invalid month. Exiting.");
                return;
            }

            fromDate = new DateTime(year, month, 1);
            toDate = fromDate.AddMonths(1).AddDays(-1);
            break;

        default:
            Console.WriteLine("Invalid choice. Exiting.");
            return;
    }

    // Ask user what type of report to generate
    Console.WriteLine("\nSelect report type:");
    Console.WriteLine("1. Summary report (aggregated by user)");
    Console.WriteLine("2. Detailed session report (all individual sessions)");
    Console.Write("\nEnter your choice (1-2): ");

    var reportChoice = Console.ReadLine();

    if (reportChoice == "1")
    {
        // Summary report
        Console.WriteLine($"\nFetching summary report from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}...");
        var report = await apiClient.GetInstallationReportAsync(installationId, fromDate, toDate);

        if (report == null)
        {
            Console.WriteLine("No report data received.");
            return;
        }

        // Display report
        Console.WriteLine($"\nInstallation: {report.InstallationName}");
        Console.WriteLine($"Address: {report.InstallationAddress}, {report.InstallationZipCode} {report.InstallationCity}");
        Console.WriteLine($"Time Zone: {report.InstallationTimeZone}");
        Console.WriteLine($"Report Period: {report.FromDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}");
        Console.WriteLine($"\nUser Charge Sessions:");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        if (report.TotalUserChargerReportModel.Count == 0)
        {
            Console.WriteLine("No charge sessions found in this period.");
        }
        else
        {
            foreach (var userReport in report.TotalUserChargerReportModel)
            {
                Console.WriteLine($"\nUser: {userReport.UserDetails?.FullName ?? "Unknown"} ({userReport.UserDetails?.Email ?? "N/A"})");
                Console.WriteLine($"  Sessions: {userReport.TotalChargeSessionCount}");
                Console.WriteLine($"  Energy: {userReport.TotalChargeSessionEnergy:F2} kWh");
                Console.WriteLine($"  Duration: {TimeSpan.FromSeconds(userReport.TotalChargeSessionDuration):d\\.hh\\:mm\\:ss}");
            }

            // Summary
            var totalSessions = report.TotalUserChargerReportModel.Sum(u => u.TotalChargeSessionCount);
            var totalEnergy = report.TotalUserChargerReportModel.Sum(u => u.TotalChargeSessionEnergy);
            var totalDuration = report.TotalUserChargerReportModel.Sum(u => u.TotalChargeSessionDuration);

            Console.WriteLine("\n─────────────────────────────────────────────────────────────────");
            Console.WriteLine($"Total Sessions: {totalSessions}");
            Console.WriteLine($"Total Energy: {totalEnergy:F2} kWh");
            Console.WriteLine($"Total Duration: {TimeSpan.FromSeconds(totalDuration):d\\.hh\\:mm\\:ss}");
        }
    }
    else if (reportChoice == "2")
    {
        // Detailed session report
        Console.WriteLine($"\nFetching detailed charge sessions from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}...");
        var sessions = await apiClient.GetChargeHistoryAsync(installationId, fromDate, toDate);

        if (sessions.Count == 0)
        {
            Console.WriteLine("No charge sessions found in this period.");
            return;
        }

        Console.WriteLine($"\nFound {sessions.Count} charge session(s)");
        Console.WriteLine("═════════════════════════════════════════════════════════════════\n");

        foreach (var session in sessions.OrderBy(s => s.StartDateTime))
        {
            var duration = session.EndDateTime - session.StartDateTime;
            Console.WriteLine($"Session: {session.Id}");
            Console.WriteLine($"  User: {session.UserFullName} ({session.UserEmail})");
            Console.WriteLine($"  Charger: {session.ChargerName} ({session.DeviceName})");
            Console.WriteLine($"  Start: {session.StartDateTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  End: {session.EndDateTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  Duration: {duration.Days}.{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}");
            Console.WriteLine($"  Energy: {session.Energy:F2} kWh");
            Console.WriteLine("─────────────────────────────────────────────────────────────────");
        }

        // Summary
        var totalEnergy = sessions.Sum(s => s.Energy);
        var totalDuration = sessions.Sum(s => (s.EndDateTime - s.StartDateTime).TotalSeconds);

        Console.WriteLine($"\nTotal Sessions: {sessions.Count}");
        Console.WriteLine($"Total Energy: {totalEnergy:F2} kWh");
        Console.WriteLine($"Total Duration: {TimeSpan.FromSeconds(totalDuration):d\\.hh\\:mm\\:ss}");

        // Ask if user wants to export to Excel
        Console.Write("\nExport to Excel? (y/n): ");
        var exportChoice = Console.ReadLine()?.ToLower();

        if (exportChoice == "y" || exportChoice == "yes")
        {
            var templatePath = "template.xlsx";
            if (!File.Exists(templatePath))
            {
                Console.WriteLine($"Warning: Template file '{templatePath}' not found. Please create a template.xlsx file.");
            }
            else
            {
                var outputFileName = $"ZaptecReport_{fromDate:yyyy-MM}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), outputFileName);

                var excelService = new ExcelExportService();
                excelService.ExportToExcel(sessions, templatePath, outputPath, fromDate, toDate);

                Console.WriteLine($"\nExcel report saved to: {outputPath}");
            }
        }
    }
    else
    {
        Console.WriteLine("Invalid choice. Exiting.");
        return;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\nError: {ex.Message}");
    Environment.Exit(1);
}