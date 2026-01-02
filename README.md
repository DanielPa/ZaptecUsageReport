# Zaptec Usage Report

A .NET console application that generates usage reports from the Zaptec API for EV charging installations.

## Features

- OAuth 2.0 authentication with Zaptec API
- Fetches installation charge history reports
- Displays per-user charging statistics (sessions, energy, duration)
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
2. Fetch usage data for the last 30 days
3. Display a formatted report with:
   - Installation details
   - Per-user charging statistics
   - Total summary of all sessions

## Example Output

```
Zaptec Usage Report Generator
==============================

Authenticating...
Authentication successful!

Fetching usage report from 2025-12-03 to 2026-01-02...

Installation: My EV Charging Station
Address: Main Street 123, 12345 City
Time Zone: Europe/Oslo
Report Period: 2025-12-03 to 2026-01-02

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
