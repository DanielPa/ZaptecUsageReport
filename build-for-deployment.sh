#!/bin/bash
set -e

# Build script for local preparation before LXC deployment
# This script builds the application locally so you don't need SDK in the container

echo "======================================"
echo "Building Zaptec Report for Deployment"
echo "======================================"
echo ""

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PUBLISH_DIR="$SCRIPT_DIR/publish"

# Clean previous build
if [ -d "$PUBLISH_DIR" ]; then
    echo "Cleaning previous build..."
    rm -rf "$PUBLISH_DIR"
fi

# Build the application
echo "Building application for linux-x64..."
cd "$SCRIPT_DIR/ZaptecUsageReport"
dotnet publish -c Release -r linux-x64 --self-contained false -o "$PUBLISH_DIR"

echo ""
echo "======================================"
echo "Build Complete!"
echo "======================================"
echo ""
echo "The application has been built to: $PUBLISH_DIR"
echo ""
echo "Next steps:"
echo "  1. Copy the entire project folder to your LXC container"
echo "  2. Run ./deploy.sh in the container"
echo "  3. The deploy script will use the pre-built binaries from the 'publish' folder"
echo ""
echo "Example:"
echo "  scp -r $SCRIPT_DIR root@<container-ip>:/tmp/"
echo "  ssh root@<container-ip>"
echo "  cd /tmp/ZaptecUsageReport"
echo "  ./deploy.sh"
echo ""