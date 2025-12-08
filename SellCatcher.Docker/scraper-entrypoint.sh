#!/bin/bash
set -e  # Exit on error

echo "[$(date)] ========================================="
echo "[$(date)] Starting Scraper Service"
echo "[$(date)] ========================================="

# Install Playwright browsers if not already installed
if [ ! -f "/ms-playwright/.playwright-ready" ]; then
    echo "[$(date)] Installing Playwright browsers..."
    
    # Use the official Playwright install command
    playwright install chromium firefox --with-deps || {
        echo "[$(date)] WARNING: Playwright install failed, but continuing..."
    }
    
    # Mark as installed
    touch /ms-playwright/.playwright-ready
    echo "[$(date)] Playwright browsers installed"
else
    echo "[$(date)] Playwright browsers already installed"
fi

# Wait for API to be ready
echo "[$(date)] Waiting for API to become healthy..."
MAX_WAIT=60
WAITED=0
until curl -f http://api:5000/health 2>/dev/null; do
    if [ $WAITED -ge $MAX_WAIT ]; then
        echo "[$(date)] ERROR: API failed to become healthy after ${MAX_WAIT}s"
        exit 1
    fi
    echo "[$(date)] API not ready yet, waiting... (${WAITED}s/${MAX_WAIT}s)"
    sleep 5
    WAITED=$((WAITED + 5))
done

echo "[$(date)] ✓ API is healthy and ready!"

# Run initial scraper job (but don't fail if it errors)
echo "[$(date)] ========================================="
echo "[$(date)] Running INITIAL scraper job..."
echo "[$(date)] ========================================="

curl -X POST http://api:5000/api/scraper/run \
    --connect-timeout 30 \
    --max-time 7200 \
    -H "Content-Type: application/json" \
    -v 2>&1 | tee /tmp/scraper-initial.log || {
    echo "[$(date)] WARNING: Initial scraper run failed (this is OK for first run)"
    echo "[$(date)] Check logs above for details"
}

echo "[$(date)] Initial scraper job completed"

# Main loop - run scraper every 24 hours
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
    
    curl -X POST http://api:5000/api/scraper/run \
        --connect-timeout 30 \
        --max-time 7200 \
        -H "Content-Type: application/json" \
        -v 2>&1 | tee /tmp/scraper-scheduled-${LOOP_COUNT}.log || {
        echo "[$(date)] WARNING: Scheduled scraper run failed"
        echo "[$(date)] Will retry in 24 hours"
    }
    
    echo "[$(date)] Scheduled scraper job completed"
    LOOP_COUNT=$((LOOP_COUNT + 1))
done