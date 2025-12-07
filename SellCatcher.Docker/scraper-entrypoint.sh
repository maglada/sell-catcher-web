#!/bin/bash
set -e

echo "[$(date)] Starting scraper service..."

# Check if Playwright browsers are installed
if [ ! -d "/ms-playwright/chromium-1140" ] || [ ! -d "/ms-playwright/firefox-1463" ]; then
    echo "[$(date)] Installing Playwright browsers (this may take 5-10 minutes)..."
    
    apt-get update
    apt-get install -y wget unzip
    
    # Create directories
    mkdir -p /ms-playwright/chromium-1140
    mkdir -p /ms-playwright/firefox-1463
    
    # Download and install Chromium
    echo "[$(date)] Downloading Chromium..."
    wget -q https://playwright.azureedge.net/builds/chromium/1140/chromium-linux.zip -O /tmp/chromium.zip
    echo "[$(date)] Extracting Chromium..."
    unzip -q /tmp/chromium.zip -d /ms-playwright/chromium-1140/
    chmod +x /ms-playwright/chromium-1140/chrome-linux/chrome
    rm /tmp/chromium.zip
    
    # Download and install Firefox
    echo "[$(date)] Downloading Firefox..."
    wget -q https://playwright.azureedge.net/builds/firefox/1463/firefox-ubuntu-22.04.zip -O /tmp/firefox.zip
    echo "[$(date)] Extracting Firefox..."
    unzip -q /tmp/firefox.zip -d /ms-playwright/firefox-1463/
    chmod +x /ms-playwright/firefox-1463/firefox/firefox
    rm /tmp/firefox.zip
    
    echo "[$(date)] Playwright browsers installed successfully"
else
    echo "[$(date)] Playwright browsers already installed"
fi

# Wait for API to be ready
echo "[$(date)] Waiting for API..."
until curl -f http://api:5000/health 2>/dev/null; do
    echo "[$(date)] API not ready yet, waiting..."
    sleep 5
done
echo "[$(date)] API is ready!"

# Run immediately on startup
echo "[$(date)] Running initial scraper job..."
curl -X POST http://api:5000/api/scraper/run \
    --connect-timeout 30 \
    --max-time 7200 \
    -H "Content-Type: application/json" \
    -v || echo "[$(date)] Initial scraper failed"

# Main loop: run every 24 hours
while true; do
    echo "[$(date)] Sleeping for 24 hours..."
    sleep 86400
    
    echo "[$(date)] Running scheduled scraper job..."
    curl -X POST http://api:5000/api/scraper/run \
        --connect-timeout 30 \
        --max-time 7200 \
        -H "Content-Type: application/json" \
        -v || echo "[$(date)] Scraper failed"
done