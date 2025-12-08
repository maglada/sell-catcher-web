#!/bin/bash
set -e

echo "[$(date)] Starting service..."

# Playwright 1.55.0 uses these specific paths
CHROMIUM_REV="1187"
FIREFOX_REV="1490"

CHROMIUM_DIR="/ms-playwright/chromium_headless_shell-${CHROMIUM_REV}"
FIREFOX_DIR="/ms-playwright/firefox-${FIREFOX_REV}"

if [ ! -f "/ms-playwright/.playwright-installed" ]; then
    echo "[$(date)] Installing Playwright browsers for version 1.55.0..."
    
    apt-get update
    apt-get install -y wget unzip
    
    # Install Chromium Headless Shell (new in 1.55.0)
    if [ ! -d "$CHROMIUM_DIR" ]; then
        echo "[$(date)] Downloading Chromium Headless Shell ${CHROMIUM_REV}..."
        mkdir -p "$CHROMIUM_DIR"
        wget -q "https://playwright.azureedge.net/builds/chromium/${CHROMIUM_REV}/chromium-headless-shell-linux.zip" -O /tmp/chromium.zip
        unzip -q /tmp/chromium.zip -d "$CHROMIUM_DIR/"
        chmod +x "$CHROMIUM_DIR/chrome-linux/headless_shell"
        rm /tmp/chromium.zip
        echo "[$(date)] Chromium installed: $CHROMIUM_DIR/chrome-linux/headless_shell"
    fi
    
    # Install Firefox
    if [ ! -d "$FIREFOX_DIR" ]; then
        echo "[$(date)] Downloading Firefox ${FIREFOX_REV}..."
        mkdir -p "$FIREFOX_DIR"
        wget -q "https://playwright.azureedge.net/builds/firefox/${FIREFOX_REV}/firefox-ubuntu-22.04.zip" -O /tmp/firefox.zip
        unzip -q /tmp/firefox.zip -d "$FIREFOX_DIR/"
        chmod +x "$FIREFOX_DIR/firefox/firefox"
        rm /tmp/firefox.zip
        echo "[$(date)] Firefox installed: $FIREFOX_DIR/firefox/firefox"
    fi
    
    touch /ms-playwright/.playwright-installed
    echo "[$(date)] Playwright browsers installed successfully"
else
    echo "[$(date)] Playwright browsers already installed"
fi

echo "[$(date)] Starting application..."
exec dotnet SellCatcher.Api.dll