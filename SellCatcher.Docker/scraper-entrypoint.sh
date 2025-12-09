#!/bin/bash
set -e

echo "[$(date)] ========================================="
echo "[$(date)] Starting Scraper Service"
echo "[$(date)] ========================================="

# Verify Playwright installation
if [ -f "/ms-playwright/.playwright-installed" ]; then
    echo "[$(date)] ✓ Playwright browsers already installed during build"
    ls -la /ms-playwright/ 2>/dev/null || echo "Warning: Playwright directory empty"
else
    echo "[$(date)] WARNING: Playwright not installed during build"
    echo "[$(date)] Attempting runtime installation..."
    
    PLAYWRIGHT_SCRIPT=$(find /app -name "playwright.ps1" 2>/dev/null | head -n 1)
    if [ -n "$PLAYWRIGHT_SCRIPT" ]; then
        pwsh "$PLAYWRIGHT_SCRIPT" install chromium firefox --with-deps || {
            echo "[$(date)] ERROR: Failed to install Playwright browsers"
            exit 1
        }
    else
        echo "[$(date)] ERROR: Cannot find playwright.ps1"
        exit 1
    fi
fi

# Verify sites folder
echo "[$(date)] Verifying sites folder..."
if [ ! -d "/app/Services/Scraper/sites" ]; then
    echo "[$(date)] ERROR: Sites folder not found!"
    exit 1
fi

SITE_FILES=$(find /app/Services/Scraper/sites -name "*.txt" | wc -l)
echo "[$(date)] Found $SITE_FILES site link files"

if [ "$SITE_FILES" -eq 0 ]; then
    echo "[$(date)] ERROR: No .txt files found in sites folder!"
    exit 1
fi

# Wait for API with detailed logging
echo "[$(date)] Waiting for API to become healthy..."
MAX_WAIT=120
WAITED=0
while true; do
    if curl -f http://api:5000/health 2>/dev/null; then
        echo "[$(date)] ✓ API is healthy!"
        break
    fi
    
    if [ $WAITED -ge $MAX_WAIT ]; then
        echo "[$(date)] ERROR: API failed to become healthy after ${MAX_WAIT}s"
        echo "[$(date)] Last curl attempt:"
        curl -v http://api:5000/health 2>&1 || true
        exit 1
    fi
    
    echo "[$(date)] API not ready yet, waiting... (${WAITED}s/${MAX_WAIT}s)"
    sleep 5
    WAITED=$((WAITED + 5))
done

# Run initial scraper job
echo "[$(date)] ========================================="
echo "[$(date)] Running INITIAL scraper job..."
echo "[$(date)] ========================================="

HTTP_CODE=$(curl -X POST http://api:5000/api/scraper/run \
    --connect-timeout 30 \
    --max-time 7200 \
    -H "Content-Type: application/json" \
    -w "%{http_code}" \
    -o /tmp/scraper-initial-response.log \
    2>/tmp/scraper-initial-error.log || echo "000")

echo "[$(date)] HTTP Response Code: $HTTP_CODE"
echo "[$(date)] Response body:"
cat /tmp/scraper-initial-response.log || echo "No response body"

if [ "$HTTP_CODE" != "200" ]; then
    echo "[$(date)] WARNING: Initial scraper returned HTTP $HTTP_CODE"
    echo "[$(date)] Error log:"
    cat /tmp/scraper-initial-error.log || echo "No error log"
else
    echo "[$(date)] ✓ Initial scraper job completed successfully"
fi

# Main loop
echo "[$(date)] ========================================="
echo "[$(date)] Entering main loop (24h interval)"
echo "[$(date)] ========================================="

LOOP_COUNT=1
while true; do
    echo "[$(date)] Sleeping for 24 hours... (Loop #${LOOP_COUNT})"
    sleep 86400
    
    echo "[$(date)] ========================================="
    echo "[$(date)] Running SCHEDULED scraper job (Loop #${LOOP_COUNT})"
    echo "[$(date)] ========================================="
    
    HTTP_CODE=$(curl -X POST http://api:5000/api/scraper/run \
        --connect-timeout 30 \
        --max-time 7200 \
        -H "Content-Type: application/json" \
        -w "%{http_code}" \
        -o /tmp/scraper-scheduled-${LOOP_COUNT}-response.log \
        2>/tmp/scraper-scheduled-${LOOP_COUNT}-error.log || echo "000")
    
    echo "[$(date)] HTTP Response Code: $HTTP_CODE"
    
    if [ "$HTTP_CODE" != "200" ]; then
        echo "[$(date)] WARNING: Scheduled scraper returned HTTP $HTTP_CODE"
    else
        echo "[$(date)] ✓ Scheduled scraper job completed"
    fi
    
    LOOP_COUNT=$((LOOP_COUNT + 1))
done