#!/bin/bash
set -e

# Zaptec Usage Report - LXC Deployment Script
# This script deploys the application to an LXC container running Debian/Ubuntu

# Configuration
APP_NAME="zaptec-report"
APP_USER="zaptec"
APP_GROUP="zaptec"
INSTALL_DIR="/opt/${APP_NAME}"
SERVICE_NAME="${APP_NAME}.service"
TIMER_NAME="${APP_NAME}.timer"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}======================================${NC}"
echo -e "${GREEN}Zaptec Report Deployment Script${NC}"
echo -e "${GREEN}======================================${NC}\n"

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo -e "${RED}Please run as root (sudo)${NC}"
    exit 1
fi

# Check if this is Debian/Ubuntu
if [ ! -f /etc/debian_version ]; then
    echo -e "${YELLOW}Warning: This script is designed for Debian/Ubuntu systems${NC}"
    read -p "Continue anyway? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

echo -e "${GREEN}Step 1: Installing dependencies${NC}"
apt-get update
apt-get install -y wget curl apt-transport-https

# Install .NET 9.0 Runtime
if ! command -v dotnet &> /dev/null; then
    echo -e "${GREEN}Installing .NET 9.0 Runtime...${NC}"
    wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    apt-get update
    apt-get install -y dotnet-runtime-9.0
else
    echo -e "${GREEN}.NET already installed${NC}"
fi

echo -e "\n${GREEN}Step 2: Creating application user and group${NC}"
if ! id -u $APP_USER > /dev/null 2>&1; then
    useradd --system --no-create-home --shell /bin/false $APP_USER
    echo -e "${GREEN}User $APP_USER created${NC}"
else
    echo -e "${GREEN}User $APP_USER already exists${NC}"
fi

echo -e "\n${GREEN}Step 3: Creating installation directory${NC}"
mkdir -p $INSTALL_DIR
chown $APP_USER:$APP_GROUP $INSTALL_DIR
chmod 750 $INSTALL_DIR

echo -e "\n${GREEN}Step 4: Building and publishing application${NC}"
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR/ZaptecUsageReport"

# Publish the application
dotnet publish -c Release -r linux-x64 --self-contained false -o "$INSTALL_DIR"

echo -e "\n${GREEN}Step 5: Setting permissions${NC}"
chown -R $APP_USER:$APP_GROUP $INSTALL_DIR
chmod -R 750 $INSTALL_DIR
chmod +x $INSTALL_DIR/ZaptecUsageReport

echo -e "\n${GREEN}Step 6: Configuring application${NC}"
echo -e "${YELLOW}Please update the following files with your configuration:${NC}"
echo -e "  - $INSTALL_DIR/appsettings.json"
echo -e "  - Template file: $INSTALL_DIR/template.xlsx"

# Copy template if it exists
if [ -f "$SCRIPT_DIR/ZaptecUsageReport/template.xlsx" ]; then
    cp "$SCRIPT_DIR/ZaptecUsageReport/template.xlsx" "$INSTALL_DIR/"
    chown $APP_USER:$APP_GROUP "$INSTALL_DIR/template.xlsx"
    echo -e "${GREEN}Template file copied${NC}"
fi

echo -e "\n${GREEN}Step 7: Setting up credentials${NC}"
echo -e "${YELLOW}You have two options for credentials:${NC}"
echo -e "  1. Environment variables (in systemd service file)"
echo -e "  2. Configuration file (appsettings.json - less secure)"
echo -e "\n${YELLOW}For environment variables, edit: /etc/systemd/system/${SERVICE_NAME}${NC}"
echo -e "${YELLOW}Add these lines under [Service]:${NC}"
echo -e "  Environment=\"Zaptec__Username=your_email@example.com\""
echo -e "  Environment=\"Zaptec__Password=your_password\""
echo -e "  Environment=\"Email__Username=smtp_username\""
echo -e "  Environment=\"Email__Password=smtp_password\""

echo -e "\n${GREEN}Step 8: Installing systemd service and timer${NC}"
cp "$SCRIPT_DIR/systemd/${SERVICE_NAME}" /etc/systemd/system/
cp "$SCRIPT_DIR/systemd/${TIMER_NAME}" /etc/systemd/system/
chmod 644 /etc/systemd/system/${SERVICE_NAME}
chmod 644 /etc/systemd/system/${TIMER_NAME}

# Reload systemd
systemctl daemon-reload

echo -e "\n${GREEN}Step 9: Enabling and starting timer${NC}"
systemctl enable ${TIMER_NAME}
systemctl start ${TIMER_NAME}

echo -e "\n${GREEN}======================================${NC}"
echo -e "${GREEN}Deployment Complete!${NC}"
echo -e "${GREEN}======================================${NC}\n"

echo -e "${YELLOW}Next steps:${NC}"
echo -e "  1. Edit configuration: nano $INSTALL_DIR/appsettings.json"
echo -e "  2. Add credentials to systemd service: nano /etc/systemd/system/${SERVICE_NAME}"
echo -e "  3. Reload systemd: systemctl daemon-reload"
echo -e "  4. Test the service manually: systemctl start ${SERVICE_NAME}"
echo -e "  5. Check logs: journalctl -u ${SERVICE_NAME} -f"
echo -e "  6. Check timer status: systemctl status ${TIMER_NAME}"
echo -e "  7. List next scheduled runs: systemctl list-timers ${TIMER_NAME}"

echo -e "\n${YELLOW}Important:${NC}"
echo -e "  - The report will run automatically on the 1st of each month at 08:00 AM"
echo -e "  - Make sure your template.xlsx file is in $INSTALL_DIR"
echo -e "  - Make sure SMTP settings are correctly configured"
echo -e "  - Check firewall rules for SMTP port (usually 587 or 465)"

echo -e "\n${GREEN}Installation directory: ${INSTALL_DIR}${NC}"
echo -e "${GREEN}Service: systemctl status ${SERVICE_NAME}${NC}"
echo -e "${GREEN}Timer: systemctl status ${TIMER_NAME}${NC}\n"