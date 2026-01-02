# Zaptec Usage Report

A .NET console application that generates usage reports from the Zaptec API for EV charging installations.

## Features

- OAuth 2.0 authentication with Zaptec API
- **Two report types:**
  - **Summary Report**: Aggregated statistics per user (total sessions, energy, duration)
  - **Detailed Session Report**: Individual charge session details with timestamps, duration, and energy consumption
- Flexible date range selection (current month, last month, or custom month)
- Automatic pagination for large datasets
- Secure credential management using user secrets

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

2. **Update the Installation ID** in `appsettings.json`:

Edit the file and replace the `InstallationId` with your actual installation ID:

```json
{
  "Zaptec": {
    "ApiBaseUrl": "https://api.zaptec.com",
    "InstallationId": "your-installation-id-here"
  }
}
```

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
```

## Project Structure

- `Program.cs` - Main entry point and report display logic
- `Services/ZaptecApiClient.cs` - Zaptec API client with authentication and data fetching
- `Models/` - Data models for API requests and responses
- `appsettings.json` - Configuration file (non-sensitive settings)

## Security Notes

- Credentials are stored using .NET user secrets, not in source code
- Never commit credentials to version control
- The `.gitignore` file already excludes user secrets and sensitive files

## API Documentation

- [Zaptec API Authentication](https://docs.zaptec.com/docs/api-authentication)
- [API Usage Guidelines](https://docs.zaptec.com/docs/api-usage-guidelines)
- [API Fair Use Policy](https://docs.zaptec.com/docs/api-fair-use-policy)
