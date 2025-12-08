#!/bin/bash
set -e

echo "[$(date)] Starting API service..."

# Quick check if browsers exist, if not install them quickly
if [ ! -f "/ms-playwright/.ready" ]; then
    echo "[$(date)] Installing Playwright browsers..."
    playwright install chromium firefox --with-deps 2>/dev/null || echo "Playwright install warning (may already be installed)"
    touch /ms-playwright/.ready
fi

echo "[$(date)] Starting application..."
exec dotnet SellCatcher.Api.dll