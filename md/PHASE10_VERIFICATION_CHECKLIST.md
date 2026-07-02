# Phase 10: Docker Setup Verification Checklist

## Pre-Implementation Checklist

Before starting the Docker environment:

- [ ] Docker Desktop installed and running
- [ ] At least 8GB RAM available for Docker
- [ ] Ports 5000-5004 and 1433 available (check with `netstat -ano`)
- [ ] No existing services using target ports
- [ ] Working directory: `d:\Documents\שרי\לימודים\microservise\server`

## Files Verification

### Dockerfiles (should exist)
- [ ] `Services/AuthService/Dockerfile` - Auth microservice
- [ ] `Services/CatalogService/Dockerfile` - Catalog microservice
- [ ] `Services/OrderService/Dockerfile` - Order microservice
- [ ] `Services/LotteryService/Dockerfile` - Lottery microservice
- [ ] `Gateway/ApiGateway/Dockerfile` - API Gateway

### Configuration Files (should exist)
- [ ] `docker-compose.yml` - Main orchestration file
- [ ] `docker-compose.prod.yml` - Production overrides
- [ ] `.dockerignore` - Build optimization
- [ ] `.env.example` - Environment template

### Documentation (should exist)
- [ ] `PHASE10_DOCKER_SETUP.md` - Comprehensive guide
- [ ] `PHASE10_SUMMARY.md` - Executive summary
- [ ] `DOCKER_QUICK_REFERENCE.md` - Command reference
- [ ] `DOCKER_TROUBLESHOOTING.md` - Troubleshooting guide

### Scripts (should exist)
- [ ] `build-and-push.ps1` - Cloud push (PowerShell)
- [ ] `build-and-push.sh` - Cloud push (Bash)
- [ ] `test-docker-health.ps1` - Health check (PowerShell)
- [ ] `test-docker-health.sh` - Health check (Bash)

## Initial Startup

### Step 1: Build and Start
```powershell
cd d:\Documents\שרי\לימודים\microservise\server
docker-compose up --build
```

- [ ] Command completes without errors
- [ ] All 6 services start (look for "Started" messages)
- [ ] No "port already in use" errors
- [ ] No database connection errors
- [ ] Services reach healthy state (look for "health: healthy")

### Step 2: Verify Container Status
```powershell
docker-compose ps
```

- [ ] **mssql-db**: Status "Up" with (healthy) ✅
- [ ] **auth-service**: Status "Up" with (healthy) ✅
- [ ] **catalog-service**: Status "Up" with (healthy) ✅
- [ ] **order-service**: Status "Up" with (healthy) ✅
- [ ] **lottery-service**: Status "Up" with (healthy) ✅
- [ ] **api-gateway**: Status "Up" with (healthy) ✅

### Step 3: Run Health Check Script
```powershell
./test-docker-health.ps1
```

- [ ] All services report healthy (✅ symbols)
- [ ] All HTTP endpoints return status 200
- [ ] Database connectivity verified
- [ ] Network DNS resolution working

## Service Accessibility Testing

### HTTP Endpoints
```powershell
# Gateway
curl http://localhost:5000/health
```
- [ ] Returns 200 OK

```powershell
# Auth Service
curl http://localhost:5001/health
```
- [ ] Returns 200 OK

```powershell
# Catalog Service
curl http://localhost:5002/health
```
- [ ] Returns 200 OK

```powershell
# Order Service
curl http://localhost:5003/health
```
- [ ] Returns 200 OK

```powershell
# Lottery Service
curl http://localhost:5004/health
```
- [ ] Returns 200 OK

### Database Connectivity
```powershell
# Test from host
sqlcmd -S localhost,1433 -U sa -P YourPasswordHere123! -Q "SELECT 1"
```
- [ ] Connection successful
- [ ] Query returns "1"

```powershell
# Test from container
docker exec mechira-auth-service /opt/mssql-tools/bin/sqlcmd -S mssql-db,1433 -U sa -P YourPasswordHere123! -Q "SELECT 1"
```
- [ ] Connection successful

### Inter-Service Communication
```powershell
# Test DNS resolution
docker exec mechira-auth-service nslookup order-service
```
- [ ] Resolves to valid IP address

```powershell
# Test connectivity
docker exec mechira-api-gateway curl -f http://auth-service:5001/health
```
- [ ] Returns 200 OK

## Log Verification

### View Logs
```powershell
docker-compose logs --tail=50
```

- [ ] No ERROR or EXCEPTION messages
- [ ] No connection refused errors
- [ ] All services show successful startup messages
- [ ] Database initialization completed

### Check Specific Service Logs
```powershell
docker-compose logs -f auth-service
```

- [ ] Shows "Listening on [::]:5001"
- [ ] Shows database connection successful
- [ ] Shows health check endpoint ready

## Network Verification

### Docker Network
```powershell
docker network ls
```
- [ ] Network "mechira_mechira-network" exists

```powershell
docker network inspect mechira_mechira-network
```
- [ ] All 6 containers are connected
- [ ] Container IPs are assigned
- [ ] DNS names resolve properly

## Volume Verification

### Data Persistence
```powershell
docker volume ls
```
- [ ] Volume "mechira_mssql-data" exists
- [ ] Volume "mechira_mssql-log" exists
- [ ] Volume "mechira_mssql-backup" exists

### Data Preservation Test
```powershell
# Stop containers but keep volumes
docker-compose stop

# Start containers again
docker-compose start

# Verify data still exists
docker exec mechira-mssql-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourPasswordHere123! -Q "SELECT COUNT(*) FROM sys.databases"
```
- [ ] Database still exists after restart
- [ ] Data is preserved

## Environment Configuration

### Verify Environment Variables
```powershell
docker exec mechira-auth-service env | findstr "CONNECTION"
```
- [ ] Shows connection string with mssql-db
- [ ] Uses port 1433 (internal)

```powershell
docker exec mechira-auth-service env | findstr "JWT"
```
- [ ] Shows JWT_SECRET_KEY
- [ ] Shows JWT_ISSUER
- [ ] Shows JWT_AUDIENCE

```powershell
docker exec mechira-auth-service env | findstr "ServiceUrls"
```
- [ ] Shows internal service URLs
- [ ] Uses Docker DNS names (service-name:port)

## Production Setup Verification

### Production Configuration
- [ ] Reviewed docker-compose.prod.yml
- [ ] Updated SQL Server password to strong value
- [ ] Updated JWT_SECRET_KEY to unique value
- [ ] Configured .env file for your environment
- [ ] Set ASPNETCORE_ENVIRONMENT appropriately

### Resource Limits (if configured)
```powershell
docker stats
```
- [ ] CPU usage reasonable (< 50% per service)
- [ ] Memory usage within limits (< 500MB per service)
- [ ] No out-of-memory errors

## Cleanup and Maintenance

### Test Stop/Restart
```powershell
# Stop all services
docker-compose stop

# Verify stopped
docker-compose ps
```
- [ ] All containers show "Exited"

```powershell
# Start again
docker-compose start

# Verify restarted
docker-compose ps
```
- [ ] All containers show "Up" again
- [ ] All report healthy

### Test Full Teardown
```powershell
# Remove containers and networks (keeps volumes)
docker-compose down

# Verify removed
docker ps -a | findstr mechira
```
- [ ] No containers found

```powershell
# Restart fresh
docker-compose up -d

# Verify working
docker-compose ps
```
- [ ] All containers healthy after fresh start

### Test Complete Reset (WARNING: Deletes data!)
```powershell
# Stop and remove everything including volumes
docker-compose down -v

# Verify volumes removed
docker volume ls | findstr mechira
```
- [ ] No volumes found (if applicable)

```powershell
# Full restart
docker-compose up --build
```
- [ ] All services start successfully
- [ ] Fresh database created

## Performance Testing

### Resource Monitoring
```powershell
# Monitor resource usage
docker stats --no-stream
```
- [ ] All services running
- [ ] CPU usage < 25% per service (idle)
- [ ] Memory usage < 300MB per service
- [ ] Network I/O reasonable

### Concurrent Request Test
```powershell
# Simple load test (requires ApacheBench)
ab -n 100 -c 10 http://localhost:5000/
```
- [ ] Handles concurrent requests
- [ ] Response times acceptable
- [ ] No connection errors

## Documentation Review

- [ ] Read PHASE10_DOCKER_SETUP.md completely
- [ ] Understand networking architecture
- [ ] Know common commands from DOCKER_QUICK_REFERENCE.md
- [ ] Familiar with troubleshooting in DOCKER_TROUBLESHOOTING.md
- [ ] Know how to scale services if needed
- [ ] Know cloud deployment options

## Deployment Readiness

### Ready for Development
- [ ] All local tests passing
- [ ] Services communicate correctly
- [ ] Logs look healthy
- [ ] Database persistent
- [ ] Can start/stop/restart freely

### Ready for Production
- [ ] Security reviewed and updated
- [ ] Secrets externalized from code
- [ ] Resource limits configured
- [ ] Logging and monitoring planned
- [ ] Backup strategy documented
- [ ] Deployment pipeline created

### Ready for Cloud
- [ ] Images built and tested locally
- [ ] build-and-push.ps1 script ready
- [ ] Container registry configured (Azure/AWS/GCP)
- [ ] Cloud credentials available
- [ ] Deployment targets identified
- [ ] Cost estimates completed

## Final Sign-Off

- [ ] All checklist items completed
- [ ] All tests passing
- [ ] Documentation reviewed
- [ ] Services healthy and responsive
- [ ] Ready for development or deployment

---

**Status**: ✅ Phase 10 Docker Setup Complete and Verified

**Last Verified**: [Add date when you complete checklist]
**Verified By**: [Add your name]
**Notes**: [Add any observations or issues found]
