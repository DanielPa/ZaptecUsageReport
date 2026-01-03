using Microsoft.Extensions.Configuration;
using ZaptecUsageReport.Services;

// Check if running in service mode
var isServiceMode = args.Length > 0 && args[0] == "--service";

// Load configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

// Get configuration values
var apiBaseUrl = configuration["Zaptec:ApiBaseUrl"] ?? throw new Exception("ApiBaseUrl not configured");
var installationId = configuration["Zaptec:InstallationId"] ?? throw new Exception("InstallationId not configured");
var username = configuration["Zaptec:Username"] ?? throw new Exception("Username not configured. Set with: dotnet user-secrets set \"Zaptec:Username\" \"your_email@example.com\"");
var password = configuration["Zaptec:Password"] ?? throw new Exception("Password not configured. Set with: dotnet user-secrets set \"Zaptec:Password\" \"your_password\"");

try
{
    if (!isServiceMode)
    {
        Console.WriteLine("Zaptec Usage Report Generator");
        Console.WriteLine("==============================\n");
    }

    // Initialize API client
    var apiClient = new ZaptecApiClient(apiBaseUrl);

    // Authenticate
    if (!isServiceMode)
    {
        Console.WriteLine("Authenticating...");
    }
    await apiClient.AuthenticateAsync(username, password);
    if (!isServiceMode)
    {
        Console.WriteLine("Authentication successful!\n");
    }

    // Get month selection from user or use last month in service mode
    DateTime fromDate, toDate;

    if (isServiceMode)
    {
        // Service mode: Always generate report for the previous complete month
        var lastMonth = DateTime.Now.AddMonths(-1);
        fromDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
        toDate = fromDate.AddMonths(1).AddDays(-1);
        Console.WriteLine($"[Service Mode] Generating report for {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");
    }
    else
    {
        // Interactive mode
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
    }

    // In service mode, always generate detailed report with Excel export
    // In interactive mode, ask user
    var reportChoice = isServiceMode ? "2" : null;

    if (!isServiceMode)
    {
        // Ask user what type of report to generate
        Console.WriteLine("\nSelect report type:");
        Console.WriteLine("1. Summary report (aggregated by user)");
        Console.WriteLine("2. Detailed session report (all individual sessions)");
        Console.Write("\nEnter your choice (1-2): ");

        reportChoice = Console.ReadLine();
    }

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
        if (!isServiceMode)
        {
            Console.WriteLine($"\nFetching detailed charge sessions from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}...");
        }
        var sessions = await apiClient.GetChargeHistoryAsync(installationId, fromDate, toDate);

        if (sessions.Count == 0)
        {
            Console.WriteLine("No charge sessions found in this period.");
            if (isServiceMode)
            {
                Environment.Exit(0);
            }
            return;
        }

        if (!isServiceMode)
        {
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
        }

        // Summary
        var totalEnergy = sessions.Sum(s => s.Energy);
        var totalDuration = sessions.Sum(s => (s.EndDateTime - s.StartDateTime).TotalSeconds);

        if (!isServiceMode)
        {
            Console.WriteLine($"\nTotal Sessions: {sessions.Count}");
            Console.WriteLine($"Total Energy: {totalEnergy:F2} kWh");
            Console.WriteLine($"Total Duration: {TimeSpan.FromSeconds(totalDuration):d\\.hh\\:mm\\:ss}");
        }

        // In service mode, always export both Excel and PDF and send email
        // In interactive mode, ask user
        var shouldExport = isServiceMode;
        var exportFormat = "both"; // Default for service mode: generate both Excel and PDF

        if (!isServiceMode)
        {
            Console.WriteLine("\nExport options:");
            Console.WriteLine("1. Excel (.xlsx)");
            Console.WriteLine("2. PDF (.pdf)");
            Console.WriteLine("3. Both (Excel + PDF)");
            Console.WriteLine("4. Skip export");
            Console.Write("\nEnter your choice (1-4): ");
            var exportChoice = Console.ReadLine()?.ToLower();

            shouldExport = exportChoice == "1" || exportChoice == "2" || exportChoice == "3";
            exportFormat = exportChoice switch
            {
                "1" => "excel",
                "2" => "pdf",
                "3" => "both",
                _ => "none"
            };
        }

        if (shouldExport)
        {
            var installationName = sessions.FirstOrDefault()?.InstallationName ?? "Unknown Installation";
            var baseFileName = $"ZaptecReport_{fromDate:yyyy-MM}_{DateTime.Now:yyyyMMdd_HHmmss}";
            var outputDir = isServiceMode
                ? "/tmp"
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            string? excelPath = null;
            string? pdfPath = null;

            // Generate Excel if requested
            if (exportFormat == "excel" || exportFormat == "both")
            {
                var templatePath = "template.xlsx";
                if (!File.Exists(templatePath))
                {
                    Console.WriteLine($"Error: Template file '{templatePath}' not found. Please create a template.xlsx file.");
                    if (exportFormat == "excel")
                    {
                        Environment.Exit(1);
                    }
                    Console.WriteLine("Skipping Excel export...");
                }
                else
                {
                    var excelFileName = $"{baseFileName}.xlsx";
                    excelPath = Path.Combine(outputDir, excelFileName);

                    var excelService = new ExcelExportService();
                    excelService.ExportToExcel(sessions, templatePath, excelPath, fromDate, toDate, installationName);

                    if (isServiceMode)
                    {
                        Console.WriteLine($"[Service Mode] Excel report created: {excelPath}");
                    }
                    else
                    {
                        Console.WriteLine($"\nExcel report saved to: {excelPath}");
                    }
                }
            }

            // Generate PDF if requested
            if (exportFormat == "pdf" || exportFormat == "both")
            {
                var pdfFileName = $"{baseFileName}.pdf";
                pdfPath = Path.Combine(outputDir, pdfFileName);

                // Get pricing and report info from configuration
                var costPerKwhString = configuration["Pricing:CostPerKwh"] ?? "0.25";
                var costPerKwh = double.Parse(costPerKwhString.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
                var employee = configuration["ReportInfo:Employee"] ?? "";
                var address = configuration["ReportInfo:Address"] ?? "";
                var licensePlate = configuration["ReportInfo:VehicleLicensePlate"] ?? "";
                var model = configuration["ReportInfo:VehicleModel"] ?? "";

                var pdfService = new PdfExportService(costPerKwh, employee, address, licensePlate, model);
                pdfService.GeneratePdfReport(sessions, installationName, fromDate, toDate, pdfPath);

                if (isServiceMode)
                {
                    Console.WriteLine($"[Service Mode] PDF report created: {pdfPath}");
                }
                else
                {
                    Console.WriteLine($"PDF report saved to: {pdfPath}");
                }
            }

            // Send email in service mode (attach both Excel and PDF if available)
            if (isServiceMode && (excelPath != null || pdfPath != null))
            {
                // Prepare attachments list
                var attachments = new List<(string filePath, string fileName)>();
                if (excelPath != null)
                {
                    attachments.Add((excelPath, Path.GetFileName(excelPath)));
                }
                if (pdfPath != null)
                {
                    attachments.Add((pdfPath, Path.GetFileName(pdfPath)));
                }

                // Get email configuration
                var smtpServer = configuration["Email:SmtpServer"] ?? throw new Exception("Email:SmtpServer not configured");
                var smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "465");
                var useSsl = bool.Parse(configuration["Email:UseSsl"] ?? "true");
                var emailUsername = configuration["Email:Username"] ?? configuration["Zaptec:Username"] ?? throw new Exception("Email:Username not configured");
                var emailPassword = configuration["Email:Password"] ?? configuration["Zaptec:Password"] ?? throw new Exception("Email:Password not configured");
                var fromEmail = configuration["Email:FromEmail"] ?? throw new Exception("Email:FromEmail not configured");
                var fromName = configuration["Email:FromName"] ?? "Zaptec Report Service";
                var toEmails = configuration.GetSection("Email:ToEmails").Get<string[]>() ?? throw new Exception("Email:ToEmails not configured");
                var ccEmails = configuration.GetSection("Email:CcEmails").Get<string[]>();
                var bccEmails = configuration.GetSection("Email:BccEmails").Get<string[]>();
                var subjectTemplate = configuration["Email:SubjectTemplate"] ?? "Zaptec Usage Report - {0:MMMM yyyy}";

                var emailService = new EmailService(smtpServer, smtpPort, emailUsername, emailPassword, useSsl, fromEmail, fromName);
                var subject = string.Format(subjectTemplate, fromDate);
                var emailBody = EmailService.GenerateReportEmailBody(
                    installationName,
                    fromDate,
                    toDate,
                    sessions.Count,
                    totalEnergy,
                    totalDuration,
                    hasExcel: excelPath != null,
                    hasPdf: pdfPath != null
                );

                Console.WriteLine($"[Service Mode] Sending email with {attachments.Count} attachment(s) to {string.Join(", ", toEmails)}...");
                await emailService.SendReportEmailWithAttachmentsAsync(
                    toEmails,
                    subject,
                    emailBody,
                    attachments.ToArray(),
                    ccEmails,
                    bccEmails
                );
                Console.WriteLine($"[Service Mode] Email sent successfully!");

                // Clean up temp files
                if (excelPath != null && File.Exists(excelPath))
                {
                    File.Delete(excelPath);
                    Console.WriteLine($"[Service Mode] Temporary Excel file deleted.");
                }
                if (pdfPath != null && File.Exists(pdfPath))
                {
                    File.Delete(pdfPath);
                    Console.WriteLine($"[Service Mode] Temporary PDF file deleted.");
                }
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