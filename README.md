# Zaptec Usage Report

A .NET console application that generates usage reports from the Zaptec API for EV charging installations.

## Features

- OAuth 2.0 authentication with Zaptec API
- **Two report types:**
  - **Summary Report**: Aggregated statistics per user (total sessions, energy, duration)
  - **Detailed Session Report**: Individual charge session details with timestamps, duration, and energy consumption
- **Multiple Export Formats**: Export detailed reports to Excel or PDF
  - **Excel Export**: Using customizable templates with formulas and formatting
  - **PDF Export**: Professional landscape reports with tables and headers
  - Support for exporting both formats simultaneously
- **Automated Service Mode**: Run as a scheduled service
  - Automatic monthly report generation
  - Email delivery with Excel attachment
  - Systemd timer integration for reliable scheduling
  - Perfect for LXC containers on Proxmox
- Flexible date range selection (current month, last month, or custom month)
- Automatic pagination for large datasets
- Secure credential management using user secrets or environment variables

## Prerequisites

- .NET 9.0 SDK or later
- Zaptec account with API access
- Installation ID from your Zaptec installation

## Setup

1. **Configure your credentials using user secrets** (recommended for security):

```bash
# Navigate to the project directory
cd ZaptecUsageReport

# Set your Zaptec username (email)
dotnet user-secrets set "Zaptec:Username" "your_email@example.com"

# Set your Zaptec password
dotnet user-secrets set "Zaptec:Password" "your_password"
```

2. **Update the Installation ID and Pricing** in `appsettings.json`:

Edit the file and replace the `InstallationId` with your actual installation ID and optionally adjust the cost per kWh:

```json
{
  "Zaptec": {
    "ApiBaseUrl": "https://api.zaptec.com",
    "InstallationId": "your-installation-id-here"
  },
  "Pricing": {
    "CostPerKwh": 0.25
  }
}
```

The `CostPerKwh` value is used to calculate costs in PDF reports (default: €0.25 per kWh).

3. **(Optional) Create an Excel template** for exporting detailed reports:

Create a file named `template.xlsx` in the `ZaptecUsageReport` directory with these columns:

| Column | Header | Description |
|--------|--------|-------------|
| A | Session ID | Unique session identifier |
| B | Device ID | Charger device identifier |
| C | Start Date/Time | Session start timestamp |
| D | End Date/Time | Session end timestamp |
| E | Duration | Duration in HH:MM format |
| F | Energy (kWh) | Energy consumed in kWh |
| G | Signed Session | Cryptographic signature |

You can add formulas, formatting, and charts to the template. See [TEMPLATE_INSTRUCTIONS.md](TEMPLATE_INSTRUCTIONS.md) for detailed instructions.

## Usage

```bash
# Build the project
dotnet build

# Run the application
dotnet run --project ZaptecUsageReport/ZaptecUsageReport.csproj
```

The application will:
1. Authenticate with the Zaptec API
2. Prompt you to select a date range (current month, last month, or custom month)
3. Ask you to choose between summary or detailed report
4. Display a formatted report with:
   - **Summary Report**: Installation details, per-user aggregated statistics, and totals
   - **Detailed Report**: Individual charge sessions with start/end times, duration, energy, user, and charger information

## Example Output

### Summary Report
```
Zaptec Usage Report Generator
==============================

Authenticating...
Authentication successful!

Select the month for the report:
1. Current month (so far)
2. Last month (complete)
3. Specific month (enter year and month)

Enter your choice (1-3): 2

Select report type:
1. Summary report (aggregated by user)
2. Detailed session report (all individual sessions)

Enter your choice (1-2): 1

Fetching summary report from 2025-12-01 to 2025-12-31...

Installation: My EV Charging Station
Address: Main Street 123, 12345 City
Time Zone: Europe/Oslo
Report Period: 2025-12-01 to 2025-12-31

User Charge Sessions:
─────────────────────────────────────────────────────────────────

User: John Doe (john@example.com)
  Sessions: 15
  Energy: 245.50 kWh
  Duration: 12.05:30:00

─────────────────────────────────────────────────────────────────
Total Sessions: 15
Total Energy: 245.50 kWh
Total Duration: 12.05:30:00
```

### Detailed Session Report
```
Found 3 charge session(s)
═════════════════════════════════════════════════════════════════

Session: abc123...
  User: John Doe (john@example.com)
  Charger: Main Charger (Device-001)
  Start: 2025-12-15 08:30:00
  End: 2025-12-15 12:45:00
  Duration: 0.04:15:00
  Energy: 18.50 kWh
─────────────────────────────────────────────────────────────────
Session: def456...
  User: Jane Smith (jane@example.com)
  Charger: Main Charger (Device-001)
  Start: 2025-12-16 14:20:00
  End: 2025-12-16 18:10:00
  Duration: 0.03:50:00
  Energy: 22.30 kWh
─────────────────────────────────────────────────────────────────

Total Sessions: 3
Total Energy: 65.20 kWh
Total Duration: 0.11:25:00

Export options:
1. Excel (.xlsx)
2. PDF (.pdf)
3. Both (Excel + PDF)
4. Skip export

Enter your choice (1-4): 3

Excel report saved to: /Users/username/Documents/ZaptecReport_2025-12_20260102_143025.xlsx
PDF report saved to: /Users/username/Documents/ZaptecReport_2025-12_20260102_143025.pdf
```

## Report Export

### Excel Export

The detailed session report can be exported to Excel format:

1. Create a `template.xlsx` file with the column structure (see setup step 3)
2. Run the application and select "Detailed session report"
3. When prompted, choose to export to Excel
4. The file will be saved to your Documents folder with a timestamped filename

**Features:**
- Template formulas are automatically recalculated
- Custom formatting and styling are preserved
- Charts and conditional formatting work as expected
- Output filename format: `ZaptecReport_YYYY-MM_YYYYMMDD_HHMMSS.xlsx`

For detailed template creation instructions, see [TEMPLATE_INSTRUCTIONS.md](TEMPLATE_INSTRUCTIONS.md).

### PDF Export

The detailed session report can also be exported to PDF format:

1. Run the application and select "Detailed session report"
2. When prompted, choose to export to PDF (or both formats)
3. The file will be saved to your Documents folder with a timestamped filename

**Features:**
- Professional landscape layout optimized for A4 paper
- Colored header with installation name and report period
- Summary statistics (total sessions, energy, **total cost**, duration)
- **Calculated cost column** based on configurable price per kWh (€0.25 default)
- Formatted table with all charge sessions
- Alternating row colors for better readability
- Automatic pagination with page numbers
- No template required - works out of the box
- Output filename format: `ZaptecReport_YYYY-MM_YYYYMMDD_HHMMSS.pdf`

**Cost Calculation:**
The PDF report includes a "Cost (EUR)" column that automatically calculates the cost for each charging session based on the `Pricing:CostPerKwh` value in `appsettings.json`. The total cost is also displayed in the header summary.

**Note:** PDF export uses QuestPDF under the Community License, which is free for commercial use.

## Automated Service Mode

The application can run as an automated service that generates monthly reports and sends them via email.

### Running in Service Mode

```bash
# Interactive mode (default)
dotnet run

# Service mode (automated, non-interactive)
dotnet run -- --service
```

In service mode, the application will:
1. Generate a report for the previous complete month
2. Create an Excel file using the template
3. Send the Excel file via email to configured recipients
4. Exit automatically when done

### Deployment to LXC Container

For production use, deploy the application as a systemd service on an LXC container:

```bash
# Run the deployment script
./deploy.sh
```

This will:
- Install .NET 9.0 Runtime
- Create a dedicated user account
- Deploy the application to `/opt/zaptec-report`
- Install systemd service and timer
- Configure automatic monthly execution

**For detailed deployment instructions, see [DEPLOYMENT.md](DEPLOYMENT.md).**

### Email Configuration

Add email settings to `appsettings.json`:

```json
{
  "Email": {
    "SmtpServer": "posteo.de",
    "SmtpPort": 465,
    "UseSsl": true,
    "FromEmail": "your-email@posteo.de",
    "FromName": "Zaptec Report Service",
    "ToEmails": ["recipient@example.com"],
    "CcEmails": [],
    "BccEmails": [],
    "SubjectTemplate": "Zaptec Usage Report - {0:MMMM yyyy}"
  }
}
```

**Note for Posteo users:**
- Create an App Password for SMTP access
- Visit https://posteo.de/en/help/app-passwords for instructions
- Use your Posteo email address as `Email__Username` in environment variables
- Use the generated app password as `Email__Password` in environment variables

### Scheduling

The service runs automatically on the 1st of each month at 8:00 AM using systemd timers.

To customize the schedule, edit `/etc/systemd/system/zaptec-report.timer`:

```ini
# Run on 2nd of month at 10:00 AM
OnCalendar=*-*-02 10:00:00

# Run weekly on Monday
OnCalendar=Mon *-*-* 09:00:00
```

## Project Structure

- `Program.cs` - Main entry point and report display logic
- `Services/ZaptecApiClient.cs` - Zaptec API client with authentication and data fetching
- `Services/ExcelExportService.cs` - Excel export functionality using ClosedXML
- `Services/PdfExportService.cs` - PDF export functionality using QuestPDF
- `Services/EmailService.cs` - Email service with SMTP support
- `Models/` - Data models for API requests and responses
- `appsettings.json` - Configuration file (non-sensitive settings)
- `template.xlsx` - (Optional) Excel template for exports
- `systemd/` - Systemd service and timer files
- `deploy.sh` - Automated deployment script for LXC containers
- `DEPLOYMENT.md` - Detailed deployment guide

## Security Notes

- Credentials can be stored using:
  - .NET user secrets (development)
  - Environment variables (production/systemd)
  - Configuration files (not recommended for passwords)
- Never commit credentials to version control
- Use app passwords for email providers that support them
- The `.gitignore` file already excludes user secrets and sensitive files

## API Documentation

- [Zaptec API Authentication](https://docs.zaptec.com/docs/api-authentication)
- [API Usage Guidelines](https://docs.zaptec.com/docs/api-usage-guidelines)
- [API Fair Use Policy](https://docs.zaptec.com/docs/api-fair-use-policy)

## Deployment Options

### Option 1: Interactive Desktop Use
Run the application manually on your desktop to generate ad-hoc reports.

### Option 2: Automated LXC Container (Recommended)
Deploy to an LXC container on Proxmox for automated monthly reports with email delivery.
See [DEPLOYMENT.md](DEPLOYMENT.md) for complete instructions.

### Option 3: Docker Container
Build your own Docker container using the provided application structure.

### Option 4: Windows Task Scheduler
Schedule the application on Windows using Task Scheduler with the `--service` flag.
