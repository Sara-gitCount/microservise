# Docker Troubleshooting Guide

## Common Issues and Solutions

### Issue 1: "Port Already in Use" Error

**Error Message:**
```
Error response from daemon: Ports are not available:
listen tcp 0.0.0.0:5001: bind: An attempt was made to use a socket in a way forbidden by its access rules.
```

**Causes:**
- Another application is using the port
- A previous Docker container instance is still running
- Windows networking issues

**Solutions:**

```powershell
# Find what's using port 5001
netstat -ano | findstr :5001

# Kill the process (replace PID with actual process ID)
taskkill /PID {PID} /F

# Or free up ports by stopping Docker containers
docker-compose stop
docker-compose down

# Or change the external port in docker-compose.yml
# Map a different external port:
# ports:
#   - "5011:5001"  # External:Internal mapping
```

### Issue 2: Database Connection Failures

**Error Message:**
```
Error -2146893055: A connection was successfully established with the server, 
but then an error occurred during the pre-login handshake.
```

**Causes:**
- SQL Server container not running
- Incorrect connection string
- SQL Server not fully initialized

**Solutions:**

```powershell
# Check if SQL Server container is running
docker-compose ps mssql-db

# View SQL Server logs
docker-compose logs mssql-db

# Test connection from host
sqlcmd -S localhost,1433 -U sa -P YourPasswordHere123!

# Test connection from service container
docker exec mechira-auth-service /opt/mssql-tools/bin/sqlcmd -S mssql-db,1433 -U sa -P YourPasswordHere123! -Q "SELECT 1"

# Wait longer for database initialization
# SQL Server can take 30+ seconds to start
# Add --wait flag or manually wait:
docker-compose up -d
Start-Sleep -Seconds 45

# Rebuild without using cache
docker-compose down -v
docker system prune -f
docker-compose up --build
```

### Issue 3: Service Cannot Connect to Other Services

**Error Message:**
```
Connection refused: order-service:5003
No such host is known
```

**Causes:**
- Services not on same network
- DNS resolution not working
- Service name mismatch

**Solutions:**

```powershell
# Verify services are on the network
docker network inspect mechira_mechira-network

# Test DNS resolution between containers
docker exec mechira-auth-service nslookup order-service

# Test connectivity
docker exec mechira-auth-service ping order-service

# Check service names in docker-compose.yml match references
# Use service names defined in docker-compose.yml (lowercase)

# Verify docker-compose.yml has services on same network
# All services should have:
# networks:
#   - mechira-network

# Restart networking
docker-compose down
docker network prune
docker-compose up -d
```

### Issue 4: Containers Keep Restarting

**Error Message:**
```
Container exited with code {code}
```

**Causes:**
- Application crash
- Configuration error
- Missing dependencies

**Solutions:**

```powershell
# View container exit code and error
docker-compose ps

# View service logs (last 100 lines)
docker-compose logs --tail=100 auth-service

# Follow logs in real-time
docker-compose logs -f auth-service

# Check application logs inside container
docker exec mechira-auth-service cat /app/logs/auth-service*.txt

# Validate environment variables are correct
docker exec mechira-auth-service env | findstr "CONNECTION\|JWT"

# Check if application file exists
docker exec mechira-auth-service ls -la /app
```

### Issue 5: Out of Memory or High CPU Usage

**Error Message:**
```
Docker memory limit exceeded
Cannot allocate memory
```

**Causes:**
- Not enough RAM allocated to Docker
- Memory leak in application
- Too many container instances

**Solutions:**

```powershell
# Check resource usage
docker stats

# Check Docker memory settings
Get-DockerDesktopSettings  # Windows-specific

# Increase Docker Desktop memory allocation:
# 1. Right-click Docker icon → Preferences
# 2. Go to Resources
# 3. Increase Memory to 6-8GB minimum

# Stop unnecessary containers
docker-compose stop

# Remove dangling containers and images
docker system prune

# Set resource limits in docker-compose.yml:
services:
  auth-service:
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 512M
        reservations:
          cpus: '0.25'
          memory: 256M

# Scale down services if running multiple instances
docker-compose ps | grep -c "Up"
```

### Issue 6: Health Check Failing

**Error Message:**
```
health: starting
health: unhealthy
```

**Causes:**
- Health endpoint not implemented
- Service startup too slow
- Wrong port or path

**Solutions:**

```powershell
# Manually test health endpoint
curl http://localhost:5001/health

# Check service is listening on correct port
docker exec mechira-auth-service netstat -tlnp | grep 5001

# Increase health check timeout
# In Dockerfile:
# HEALTHCHECK --interval=30s --timeout=15s --start-period=60s --retries=5

# Check if service implements /health endpoint
docker exec mechira-auth-service grep -r "health" /app/

# View health check status
docker inspect mechira-auth-service | grep -A 5 "Health"
```

### Issue 7: Network Timeout Issues

**Error Message:**
```
ConnectTimeout: Connection timeout
HttpRequestException: Operation timed out
```

**Causes:**
- Firewall blocking connections
- Service not responding
- DNS resolution slow

**Solutions:**

```powershell
# Check firewall rules
netsh advfirewall show allprofiles

# Check if port is accessible
Test-NetConnection localhost -Port 5001

# Increase timeout in application (appsettings.json)
"Timeouts": {
  "HttpClient": 30000,
  "DatabaseCommand": 30000
}

# Test inter-service connectivity
docker exec mechira-auth-service curl -v http://order-service:5003/health

# Check network bandwidth/latency
docker exec mechira-auth-service ping order-service

# Verify Docker DNS
docker exec mechira-auth-service cat /etc/resolv.conf
```

### Issue 8: Volume Mount Issues

**Error Message:**
```
Cannot find a matching Dockerfile
Cannot locate specified Dockerfile
```

**Causes:**
- Incorrect Dockerfile path in docker-compose.yml
- Dockerfile doesn't exist
- Path separators wrong

**Solutions:**

```powershell
# Verify Dockerfile exists
Test-Path "Services/AuthService/Dockerfile"

# Use correct path format (forward slashes)
# Correct:  Services/AuthService/Dockerfile
# Wrong:    Services\AuthService\Dockerfile

# Validate docker-compose.yml
docker-compose config

# Check from correct working directory
cd server
docker-compose build
```

### Issue 9: Database Migrations Not Running

**Error Message:**
```
The entity type 'Order' requires a primary key
No mapping found for property 'X'
```

**Causes:**
- Migrations not applied
- Database schema mismatch
- Entity Framework core not configured

**Solutions:**

```powershell
# Check database schema
docker exec mechira-mssql-db sqlcmd -S localhost -U sa -P YourPasswordHere123! -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES" -h -1

# Run migrations manually
docker exec mechira-auth-service dotnet ef database update -c AuthDbContext

# Check migration status
docker exec mechira-auth-service dotnet ef migrations list -c AuthDbContext

# Recreate database
docker-compose down -v
docker-compose up
```

### Issue 10: Access Denied / Permission Errors

**Error Message:**
```
Access denied
Operation not permitted
Permission denied
```

**Causes:**
- Running containers as non-root
- File permissions in volume mounts
- SELinux/AppArmor restrictions

**Solutions:**

```powershell
# Check container user
docker exec mechira-auth-service whoami

# Run with appropriate permissions
# Update Dockerfile:
# USER app:app  # Run as non-root user

# Fix volume permissions
docker exec mechira-auth-service chmod -R 755 /app/logs

# On Linux with SELinux
# Add :z flag to volumes in docker-compose.yml:
# volumes:
#   - ./logs:/app/logs:z
```

## Diagnostic Commands Cheatsheet

```powershell
# Get comprehensive system info
docker system df

# Check Docker installation
docker version
docker info

# List all containers (running and stopped)
docker ps -a

# List all networks
docker network ls

# List all volumes
docker volume ls

# Inspect service configuration
docker-compose config

# Validate docker-compose.yml syntax
docker-compose config --resolve-image-digests

# Check service logs from start
docker-compose logs --no-log-prefix service-name

# Get exit code of stopped container
docker inspect mechira-auth-service | findstr '"ExitCode"'

# Export container filesystem
docker export mechira-auth-service | tar tf -

# Get container process list with memory
docker stats --no-stream

# Watch container in real-time
docker stats mechira-auth-service

# Check container's network settings
docker inspect -f '{{json .NetworkSettings}}' mechira-auth-service | ConvertFrom-Json
```

## Debug Mode Troubleshooting

### Enable Debug Logging

**In appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Debug",
      "Microsoft.EntityFrameworkCore": "Debug"
    }
  }
}
```

### Access Debug Information

```powershell
# View application logs inside container
docker exec mechira-auth-service cat /app/logs/auth-service*.txt

# Follow logs in real-time
docker exec mechira-auth-service tail -f /app/logs/auth-service*.txt | Select-Object -Last 50

# Get detailed error stack traces
docker-compose logs --timestamps auth-service | Select-String "Exception|Error|error"
```

## Recovery Procedures

### Nuclear Option: Complete Reset

```powershell
# WARNING: This will delete all data!
# Backup database first if needed

# Stop everything
docker-compose down

# Remove all volumes
docker volume prune -f

# Remove all images
docker image prune -a -f

# Remove all networks
docker network prune -f

# Clean Docker system
docker system prune -a -f

# Rebuild from scratch
docker-compose up --build
```

### Backup Before Reset

```powershell
# Backup database from SQL Server container
docker exec mechira-mssql-db /opt/mssql-tools/bin/sqlcmd `
  -S localhost -U sa -P YourPasswordHere123! `
  -Q "BACKUP DATABASE [Mechira-sinit-microservices] TO DISK = '/var/opt/mssql/backup/backup.bak'"

# Copy backup file from container
docker cp mechira-mssql-db:/var/opt/mssql/backup/backup.bak ./backup.bak
```

## Getting Help

### Collect Diagnostic Information

```powershell
# Create diagnostic bundle
@"
=== Docker Version ===
$(docker version)

=== Docker System Info ===
$(docker system info)

=== Compose Version ===
$(docker-compose version)

=== Running Containers ===
$(docker-compose ps)

=== Service Logs ===
$(docker-compose logs --tail=50)
"@ | Out-File -FilePath "docker-diagnostics-$(Get-Date -Format 'yyyyMMdd-HHmmss').txt"

# Share this file when asking for help
```

### Common Support Resources
- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Troubleshooting](https://docs.docker.com/compose/troubleshooting/)
- [Microsoft SQL Server Docker](https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker)
- [.NET on Docker](https://docs.microsoft.com/en-us/dotnet/architecture/containerized-lifecycle/)
