# Deployment Guide - LXC Container on Proxmox

This guide explains how to deploy the Zaptec Usage Report service as an automated monthly report generator in an LXC container on Proxmox.

## Overview

The service will:
- Run automatically on the 1st of each month at 8:00 AM
- Generate a detailed Excel report for the previous month
- Send the report via email to configured recipients
- Use systemd timers for reliable scheduling

## Prerequisites

### Proxmox LXC Container Requirements
- **OS**: Debian 12 (Bookworm) or Ubuntu 22.04/24.04 LTS
- **RAM**: Minimum 512MB, recommended 1GB
- **Disk**: 2GB minimum
- **Network**: Internet access (for API calls and SMTP)
- **Firewall**: Allow outbound SMTP (port 587/465/25)

### Required Information
- Zaptec API credentials (username/password)
- Zaptec Installation ID
- SMTP server details (server, port, credentials)
- Email recipients

## Installation Steps

### 1. Create LXC Container in Proxmox

```bash
# In Proxmox web UI:
# 1. Create CT -> Select Debian 12 template
# 2. Configure: 1GB RAM, 4GB disk, 1 CPU core
# 3. Network: DHCP or static IP
# 4. Start the container
```

### 2. Access the Container

```bash
# From Proxmox host
pct enter <container-id>

# Or via SSH
ssh root@<container-ip>
```

### 3. Upload the Project

Transfer the project files to the container. You have several options:

**Option A: Using git (recommended)**
```bash
# Install git in the container
apt-get update && apt-get install -y git

# Clone the repository
cd /tmp
git clone <your-repo-url>
cd ZaptecUsageReport
```

**Option B: Using SCP from your local machine**
```bash
# From your local machine
scp -r /Users/pappd/Documents/Github/DanielPa/ZaptecUsageReport root@<container-ip>:/tmp/
```

**Option C: Using Proxmox web UI**
Upload files through the Proxmox web interface

### 4. Run the Deployment Script

```bash
cd /tmp/ZaptecUsageReport
chmod +x deploy.sh
./deploy.sh
```

The script will:
- Install .NET 9.0 Runtime
- Create application user and directories
- Build and publish the application
- Install systemd service and timer
- Configure permissions

### 5. Configure the Application

#### 5.1 Edit appsettings.json

```bash
nano /opt/zaptec-report/appsettings.json
```

Update the configuration:

```json
{
  "Zaptec": {
    "ApiBaseUrl": "https://api.zaptec.com",
    "InstallationId": "your-installation-id-here"
  },
  "Email": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "UseSsl": true,
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Zaptec Report Service",
    "ToEmails": [
      "recipient1@example.com",
      "recipient2@example.com"
    ],
    "CcEmails": [],
    "BccEmails": [],
    "SubjectTemplate": "Zaptec Usage Report - {0:MMMM yyyy}"
  }
}
```

#### 5.2 Configure Credentials

Edit the systemd service file to add credentials:

```bash
nano /etc/systemd/system/zaptec-report.service
```

Uncomment and update these lines:

```ini
Environment="Zaptec__Username=your_email@example.com"
Environment="Zaptec__Password=your_zaptec_password"
Environment="Email__Username=smtp_username"
Environment="Email__Password=smtp_app_password"
```

**Note for Gmail users:**
- Enable 2-factor authentication
- Generate an "App Password" at https://myaccount.google.com/apppasswords
- Use the app password as `Email__Password`

#### 5.3 Add the Template File

Upload your `template.xlsx` file to the container:

```bash
# From your local machine
scp template.xlsx root@<container-ip>:/opt/zaptec-report/

# Or create it directly in the container
# See TEMPLATE_INSTRUCTIONS.md for details
```

Set proper permissions:
```bash
chown zaptec:zaptec /opt/zaptec-report/template.xlsx
chmod 644 /opt/zaptec-report/template.xlsx
```

### 6. Reload and Test

```bash
# Reload systemd to pick up configuration changes
systemctl daemon-reload

# Test the service manually
systemctl start zaptec-report.service

# Check the logs
journalctl -u zaptec-report.service -f
```

If successful, you should see:
```
[Service Mode] Generating report for YYYY-MM-DD to YYYY-MM-DD
[Service Mode] Excel report created: /tmp/ZaptecReport_...
[Service Mode] Sending email to recipient@example.com...
[Service Mode] Email sent successfully!
```

### 7. Enable Automatic Scheduling

```bash
# Enable the timer (if not already enabled)
systemctl enable zaptec-report.timer
systemctl start zaptec-report.timer

# Check timer status
systemctl status zaptec-report.timer

# List next scheduled run
systemctl list-timers zaptec-report.timer
```

## Management Commands

### Check Service Status
```bash
systemctl status zaptec-report.service
```

### View Logs
```bash
# Real-time logs
journalctl -u zaptec-report.service -f

# Last 100 lines
journalctl -u zaptec-report.service -n 100

# Logs from today
journalctl -u zaptec-report.service --since today

# Logs from specific date
journalctl -u zaptec-report.service --since "2025-01-01"
```

### Manual Execution
```bash
# Trigger the report manually (useful for testing)
systemctl start zaptec-report.service
```

### Timer Management
```bash
# Check when the timer will run next
systemctl list-timers zaptec-report.timer

# Restart timer
systemctl restart zaptec-report.timer

# Disable automatic runs
systemctl stop zaptec-report.timer
systemctl disable zaptec-report.timer
```

### Update the Application

```bash
# 1. Pull latest changes (if using git)
cd /tmp/ZaptecUsageReport
git pull

# 2. Stop the timer
systemctl stop zaptec-report.timer

# 3. Rebuild and republish
cd ZaptecUsageReport
dotnet publish -c Release -r linux-x64 --self-contained false -o /opt/zaptec-report

# 4. Restart services
systemctl daemon-reload
systemctl start zaptec-report.timer

# 5. Verify
systemctl status zaptec-report.timer
```

## Troubleshooting

### Email Not Sending

**Check SMTP credentials:**
```bash
# Test SMTP connection manually
telnet smtp.gmail.com 587
```

**Common issues:**
- Wrong SMTP server or port
- App password not configured (Gmail)
- Firewall blocking outbound SMTP
- Two-factor authentication not enabled (Gmail)

**Check logs:**
```bash
journalctl -u zaptec-report.service -n 50 --no-pager
```

### Authentication Failed

**Verify credentials:**
```bash
# Check environment variables in service
systemctl show zaptec-report.service | grep Environment
```

**Test credentials manually:**
```bash
cd /opt/zaptec-report
sudo -u zaptec ./ZaptecUsageReport --service
```

### Template File Not Found

```bash
# Verify file exists
ls -la /opt/zaptec-report/template.xlsx

# Check permissions
# Should be readable by 'zaptec' user
chown zaptec:zaptec /opt/zaptec-report/template.xlsx
chmod 644 /opt/zaptec-report/template.xlsx
```

### No Data / Empty Report

- Check date range (service generates report for previous month)
- Verify Installation ID is correct
- Check Zaptec API credentials
- Ensure there were charge sessions in the previous month

### Timer Not Running

```bash
# Check if timer is enabled
systemctl is-enabled zaptec-report.timer

# Check timer status
systemctl status zaptec-report.timer

# Enable if needed
systemctl enable zaptec-report.timer
systemctl start zaptec-report.timer
```

## Security Considerations

### File Permissions
- Application directory: `750` (owner: zaptec:zaptec)
- Configuration files: `640` (owner: zaptec:zaptec)
- Template file: `644` (owner: zaptec:zaptec)

### Credentials
- Use environment variables in systemd (preferred)
- Never commit credentials to git
- Use app passwords for Gmail/Office365
- Restrict systemd service file: `chmod 600 /etc/systemd/system/zaptec-report.service`

### Network
- Firewall: Allow only outbound HTTPS (443) and SMTP (587/465)
- No inbound ports needed
- Consider using VPN or private SMTP relay

## Customization

### Change Schedule

Edit `/etc/systemd/system/zaptec-report.timer`:

```ini
# Run on 2nd of month at 10:00 AM
OnCalendar=*-*-02 10:00:00

# Run weekly on Monday at 9:00 AM
OnCalendar=Mon *-*-* 09:00:00

# Run daily at midnight
OnCalendar=daily
```

After editing:
```bash
systemctl daemon-reload
systemctl restart zaptec-report.timer
```

### Change Report Recipients

Edit `/opt/zaptec-report/appsettings.json`:

```json
"Email": {
  "ToEmails": ["new-recipient@example.com"],
  "CcEmails": ["cc@example.com"],
  "BccEmails": ["bcc@example.com"]
}
```

No restart needed, changes take effect on next run.

### Customize Email Template

The email body is generated in `EmailService.GenerateReportEmailBody()`. To customize:
1. Edit the source code
2. Rebuild and republish
3. Follow the update procedure above

## Monitoring and Notifications

### Setup Failed Run Alerts

Create `/etc/systemd/system/zaptec-report-failed@.service`:

```ini
[Unit]
Description=Alert on Zaptec Report Failure

[Service]
Type=oneshot
ExecStart=/usr/bin/systemd-cat -t zaptec-alert echo "Zaptec report failed on %H"
```

Add to `zaptec-report.service`:
```ini
[Unit]
OnFailure=zaptec-report-failed@%n.service
```

### Log Rotation

Systemd journal handles rotation automatically. To configure:

```bash
# Edit journald config
nano /etc/systemd/journald.conf

# Set max size
SystemMaxUse=500M
```

## Backup and Restore

### Backup Configuration

```bash
# Create backup directory
mkdir -p /root/zaptec-backup

# Backup configuration
cp /opt/zaptec-report/appsettings.json /root/zaptec-backup/
cp /opt/zaptec-report/template.xlsx /root/zaptec-backup/
cp /etc/systemd/system/zaptec-report.* /root/zaptec-backup/

# Backup credentials (sensitive!)
systemctl show zaptec-report.service | grep Environment > /root/zaptec-backup/env.txt
chmod 600 /root/zaptec-backup/env.txt
```

### Restore Configuration

```bash
cp /root/zaptec-backup/appsettings.json /opt/zaptec-report/
cp /root/zaptec-backup/template.xlsx /opt/zaptec-report/
cp /root/zaptec-backup/zaptec-report.* /etc/systemd/system/

systemctl daemon-reload
systemctl restart zaptec-report.timer
```

## Support

For issues with:
- **Zaptec API**: https://docs.zaptec.com
- **Application bugs**: Check the GitHub repository
- **Deployment issues**: Review logs with `journalctl -u zaptec-report.service`

## Summary Checklist

- [ ] LXC container created and running
- [ ] Application deployed via `deploy.sh`
- [ ] `appsettings.json` configured
- [ ] Credentials set in systemd service
- [ ] `template.xlsx` uploaded and accessible
- [ ] Manual test successful (`systemctl start zaptec-report.service`)
- [ ] Email received successfully
- [ ] Timer enabled (`systemctl enable zaptec-report.timer`)
- [ ] Timer running (`systemctl status zaptec-report.timer`)
- [ ] Next run scheduled (`systemctl list-timers`)
- [ ] Logs accessible (`journalctl -u zaptec-report.service`)