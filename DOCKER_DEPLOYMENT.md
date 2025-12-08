# Docker Deployment Guide

## Project Structure

```
sell-catcher-web/
├── docker-compose.yml          # Main orchestration file
├── .dockerignore                # Root ignore rules
├── SellCatcher.Api/
│   ├── Dockerfile               # API service
│   └── .dockerignore            # API ignore rules
├── SellCatcher.Blazor/
│   ├── Dockerfile               # Frontend service
│   └── .dockerignore            # Frontend ignore rules
└── SellCatcher.Docker/
    ├── Dockerfile.scraper       # Background scraper service
    ├── scraper-entrypoint.sh    # 24-hour scraper loop
    └── nginx.conf               # Nginx configuration
```

## Services Overview

### 1. **API Service** (`SellCatcher.Api`)
- **Port:** 5000
- **Built with:** .NET 8 ASP.NET Core
- **Purpose:** REST API backend
- **Health Check:** `GET /health`
- **Restart:** Unless stopped (automatic recovery)

### 2. **Frontend Service** (`SellCatcher.Blazor`)
- **Port:** 3000
- **Built with:** .NET 8 Blazor WebAssembly + Nginx
- **Purpose:** Web UI
- **Routing:** Configured for SPA (Single Page Application)
- **API Proxy:** `/api/` routes to API service
- **Depends on:** API service must be healthy

### 3. **Scraper Service** (`scraper`)
- **No exposed ports** (background service)
- **Purpose:** Runs parser every 24 hours
- **Execution:** Calls API `/api/scraper/run` endpoint
- **Sleep:** 86400 seconds (24 hours) between runs
- **Data:** Shared volume with API (`/app/data`)
- **Depends on:** API service must be healthy

## Quick Start

### Prerequisites
- Docker & Docker Compose installed
- .NET 8 SDK (for local development only)

### Deploy Full Stack
```bash
# Navigate to project root
cd /path/to/sell-catcher-web

# Build and start all services
docker-compose up -d

# View logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f api
docker-compose logs -f frontend
docker-compose logs -f scraper
```

### Access Services
- **Frontend:** http://localhost:3000
- **API:** http://localhost:5000
- **API Docs:** http://localhost:5000/swagger (if enabled)

## Advanced Usage

### Deploy Individual Services

**API only:**
```bash
docker-compose up -d api
```

**Frontend only:**
```bash
docker-compose up -d frontend
```

**Scraper only:**
```bash
docker-compose up -d scraper
```

### Monitor Services
```bash
# Check service status
docker-compose ps

# Check health
docker-compose exec api curl http://localhost:5000/health
docker-compose exec frontend wget --spider http://localhost:3000/

# View resource usage
docker stats
```

### Stop/Restart Services
```bash
# Stop all
docker-compose down

# Stop specific service
docker-compose stop api

# Restart specific service
docker-compose restart scraper

# Remove volumes (WARNING: deletes data)
docker-compose down -v
```

## Configuration

### API Environment Variables
Edit `docker-compose.yml` under `api.environment`:
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ASPNETCORE_URLS=http://+:5000
  - ConnectionString=Data Source=/app/data/sellcatcher.db
```

### Frontend Environment Variables
Edit `docker-compose.yml` under `frontend.environment`:
```yaml
environment:
  - API_URL=http://api:5000  # Internal Docker network URL
```

### Scraper Customization
Edit `scraper-entrypoint.sh` to modify:
- **Scraper endpoint:** Change `/api/scraper/run` URL
- **Interval:** Change `sleep 86400` to different seconds
- **Retry logic:** Add error handling for failed runs

### Nginx Configuration
Edit `SellCatcher.Docker/nginx.conf` to:
- Add SSL/TLS certificates
- Modify cache headers
- Add authentication
- Configure CORS

## Database & Data Persistence

- **Database location:** `/app/data/sellcatcher.db` (LiteDB)
- **Volume mapping:** `./data:/app/data` (local `data/` folder)
- **Shared between:** API and Scraper services
- **Persistent:** Data survives service restarts

To backup:
```bash
cp -r data/ backup_$(date +%Y%m%d_%H%M%S)/
```

## Troubleshooting

### Services won't start
```bash
# Check logs
docker-compose logs

# Rebuild images
docker-compose build --no-cache

# Restart fresh
docker-compose down -v
docker-compose up -d
```

### API not reachable from frontend
- Ensure API is healthy: `docker-compose exec api curl http://localhost:5000/health`
- Check network: `docker network ls` and `docker network inspect sell-catcher-web_sellcatcher-net`

### Scraper not running
- Check logs: `docker-compose logs scraper`
- Verify API endpoint exists
- Check if scraper has API connectivity: `docker-compose exec scraper curl http://api:5000/health`

### Permission issues
- Check file ownership: `ls -la SellCatcher.Docker/scraper-entrypoint.sh`
- Make executable: `chmod +x SellCatcher.Docker/scraper-entrypoint.sh`

## Production Considerations

1. **Use environment files:** Create `.env` for sensitive data
2. **Enable HTTPS:** Add SSL certificates to Nginx
3. **Set resource limits:** Add `resources` section in docker-compose.yml
4. **Configure logging:** Use Docker logging drivers
5. **Add monitoring:** Use Prometheus/Grafana or similar
6. **Backup strategy:** Regular database backups to external storage
7. **Security:** Use secrets management instead of environment variables

## Example Production docker-compose.yml Addition

```yaml
services:
  api:
    # ... existing config ...
    resources:
      limits:
        cpus: '1'
        memory: 512M
      reservations:
        cpus: '0.5'
        memory: 256M
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

## Support

For issues, check:
- Service logs: `docker-compose logs [service-name]`
- Docker daemon logs: `docker logs [container-id]`
- Ensure all ports are available (5000, 3000)
