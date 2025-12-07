#!/bin/bash
set -e

echo "[$(date)] Starting API service..."

if [ ! -d "/ms-playwright/chromium-1187" ] || [ ! -d "/ms-playwright/firefox-1490" ]; then
    echo "[$(date)] Installing Playwright browsers (this may take 5-10 minutes)..."
    
    apt-get update
    apt-get install -y wget unzip
    
    mkdir -p /ms-playwright/chromium-1187
    mkdir -p /ms-playwright/firefox-1490
    
    echo "[$(date)] Downloading Chromium..."
    wget -q https://playwright.azureedge.net/builds/chromium/1187/chromium-linux.zip -O /tmp/chromium.zip
    echo "[$(date)] Extracting Chromium..."
    unzip -q /tmp/chromium.zip -d /ms-playwright/chromium-1187/
    chmod +x /ms-playwright/chromium-1187/chrome-linux/chrome
    rm /tmp/chromium.zip
    
    echo "[$(date)] Downloading Firefox..."
    wget -q https://playwright.azureedge.net/builds/firefox/1490/firefox-ubuntu-22.04.zip -O /tmp/firefox.zip
    echo "[$(date)] Extracting Firefox..."
    unzip -q /tmp/firefox.zip -d /ms-playwright/firefox-1490/
    chmod +x /ms-playwright/firefox-1490/firefox/firefox
    rm /tmp/firefox.zip
    
    echo "[$(date)] Playwright browsers installed successfully"
else
    echo "[$(date)] Playwright browsers already installed"
fi

echo "[$(date)] Starting ASP.NET Core API..."
exec dotnet SellCatcher.Api.dll
