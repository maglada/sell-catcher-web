#!/bin/bash
set -e

# Scraper background service entrypoint
# Runs parser every 24 hours (86400 seconds)

echo "[$(date)] Starting scraper service..."

# Optional: Run scraper immediately on first startup
# dotnet SellCatcher.Api.dll --run-scraper

# Main loop: run scraper every 24 hours
while true; do
    echo "[$(date)] Running scraper job..."
    
    # Call your scraper endpoint or run scraper logic
    # This assumes you have an API endpoint or method to trigger scraping
    curl -X POST http://localhost:5000/api/scraper/run \
        --connect-timeout 30 \
        --max-time 3600 \
        -H "Content-Type: application/json" || echo "[$(date)] Scraper request failed, will retry in 24 hours"
    
    echo "[$(date)] Scraper job completed. Sleeping for 24 hours..."
    
    # Sleep for 24 hours (86400 seconds)
    sleep 86400
done
