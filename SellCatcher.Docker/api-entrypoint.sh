#!/bin/bash
set -e

echo "[$(date)] Starting API service..."
echo "[$(date)] Playwright browsers path: $PLAYWRIGHT_BROWSERS_PATH"

# Check if runtime app's Playwright version matches installed browsers
if [ ! -f "/ms-playwright/.version-matched" ]; then
    echo "[$(date)] Installing browsers for current Playwright version..."
    
    # Find the playwright.ps1 script in the published app
    PLAYWRIGHT_SCRIPT=$(find /app -name "playwright.ps1" 2>/dev/null | head -n 1)
    
    if [ -n "$PLAYWRIGHT_SCRIPT" ]; then
        echo "[$(date)] Found Playwright script: $PLAYWRIGHT_SCRIPT"
        pwsh "$PLAYWRIGHT_SCRIPT" install chromium firefox --with-deps || echo "Install warning"
        touch /ms-playwright/.version-matched
    else
        echo "[$(date)] Warning: playwright.ps1 not found, using existing browsers"
    fi
fi

ls -la /ms-playwright/ 2>/dev/null || echo "No browsers directory"

echo "[$(date)] Starting .NET application..."
exec dotnet SellCatcher.Api.dll