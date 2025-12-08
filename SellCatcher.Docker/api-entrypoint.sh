#!/bin/bash
set -e

echo "[$(date)] Starting API service..."

# Check what Playwright version is actually being used
CHROMIUM_VERSION="1140"  # Updated to match Playwright.NET version
FIREFOX_VERSION="1463"   # Updated to match Playwright.NET version

if [ ! -d "/ms-playwright/chromium-${CHROMIUM_VERSION}" ] || [ ! -d "/ms-playwright/firefox-${FIREFOX_VERSION}" ]; then
    echo "[$(date)] Installing Playwright browsers (this may take 5-10 minutes)..."
    
    apt-get update
    apt-get install -y wget unzip
    
    # Create directories
    mkdir -p /ms-playwright/chromium-${CHROMIUM_VERSION}
    mkdir -p /ms-playwright/firefox-${FIREFOX_VERSION}
    
    # Download and install Chromium
    echo "[$(date)] Downloading Chromium ${CHROMIUM_VERSION}..."
    wget -q https://playwright.azureedge.net/builds/chromium/${CHROMIUM_VERSION}/chromium-linux.zip -O /tmp/chromium.zip
    echo "[$(date)] Extracting Chromium..."
    unzip -q /tmp/chromium.zip -d /ms-playwright/chromium-${CHROMIUM_VERSION}/
    chmod +x /ms-playwright/chromium-${CHROMIUM_VERSION}/chrome-linux/chrome
    rm /tmp/chromium.zip
    
    # Download and install Firefox
    echo "[$(date)] Downloading Firefox ${FIREFOX_VERSION}..."
    wget -q https://playwright.azureedge.net/builds/firefox/${FIREFOX_VERSION}/firefox-ubuntu-22.04.zip -O /tmp/firefox.zip
    echo "[$(date)] Extracting Firefox..."
    unzip -q /tmp/firefox.zip -d /ms-playwright/firefox-${FIREFOX_VERSION}/
    chmod +x /ms-playwright/firefox-${FIREFOX_VERSION}/firefox/firefox
    rm /tmp/firefox.zip
    
    echo "[$(date)] Playwright browsers installed successfully"
    echo "[$(date)] Chromium: /ms-playwright/chromium-${CHROMIUM_VERSION}/chrome-linux/chrome"
    echo "[$(date)] Firefox: /ms-playwright/firefox-${FIREFOX_VERSION}/firefox/firefox"
else
    echo "[$(date)] Playwright browsers already installed"
fi

echo "[$(date)] Starting ASP.NET Core API..."
exec dotnet SellCatcher.Api.dll