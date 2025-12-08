#!/bin/bash
set -e

echo "[$(date)] Starting scraper service..."

CHROMIUM_REV="1187"
FIREFOX_REV="1490"
CHROMIUM_DIR="/ms-playwright/chromium_headless_shell-${CHROMIUM_REV}"
FIREFOX_DIR="/ms-playwright/firefox-${FIREFOX_REV}"

if [ ! -f "/ms-playwright/.playwright-installed" ]; then
    echo "[$(date)] Installing Playwright browsers for version 1.55.0..."
    
    apt-get update
    apt-get install -y wget unzip
    
    if [ ! -d "$CHROMIUM_DIR" ]; then
        echo "[$(date)] Downloading Chromium Headless Shell ${CHROMIUM_REV}..."
        mkdir -p "$CHROMIUM_DIR"
        wget -q "https://playwright.azureedge.net/builds/chromium/${CHROMIUM_REV}/chromium-headless-shell-linux.zip" -O /tmp/chromium.zip
        unzip -q /tmp/chromium.zip -d "$CHROMIUM_DIR/"
        chmod +x "$CHROMIUM_DIR/chrome-linux/headless_shell"
        rm /tmp/chromium.zip
    fi
    
    if [ ! -d "$FIREFOX_DIR" ]; then
        echo "[$(date)] Downloading Firefox ${FIREFOX_REV}..."
        mkdir -p "$FIREFOX_DIR"
        wget -q "https://playwright.azureedge.net/builds/firefox/${FIREFOX_REV}/firefox-ubuntu-22.04.zip" -O /tmp/firefox.zip
        unzip -q /tmp/firefox.zip -d "$FIREFOX_DIR/"
        chmod +x "$FIREFOX_DIR/firefox/firefox"
        rm /tmp/firefox.zip
    fi
    
    touch /ms-playwright/.playwright-installed
    echo "[$(date)] Playwright browsers installed successfully"
else
    echo "[$(date)] Playwright browsers already installed"
fi

# Wait for API
echo "[$(date)] Waiting for API..."
until curl -f http://api:5000/health 2>/dev/null; do
    echo "[$(date)] API not ready yet, waiting..."
    sleep 5
done
echo "[$(date)] API is ready!"

# Run immediately
echo "[$(date)] Running initial scraper job..."
curl -X POST http://api:5000/api/scraper/run \
    --connect-timeout 30 \
    --max-time 7200 \
    -H "Content-Type: application/json" \
    -v || echo "[$(date)] Initial scraper failed"

# Main loop
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