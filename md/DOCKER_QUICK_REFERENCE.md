# Docker Quick Reference for Mechira Microservices

## Prerequisites
- Docker Desktop installed and running
- At least 8GB RAM available
- Ports 5000-5004 and 1433 available

## Essential Commands

### Starting Services
```powershell
# Build and start all services
docker-compose up --build

# Start in background mode
docker-compose up -d --build

# Start services with specific environment file
docker-compose --env-file .env up -d

# Scale a specific service (run multiple instances)
docker-compose up -d --scale order-service=3
```

### Viewing Status & Logs
```powershell
# Show all running containers
docker-compose ps

# Follow logs from all services in real-time
docker-compose logs -f

# Follow logs from specific service
docker-compose logs -f auth-service

# View last 50 lines without following
docker-compose logs --tail=50

# View logs with timestamps and service names
docker-compose logs -f --timestamps

# Filter logs by service
docker-compose logs auth-service catalog-service
```

### Container Management
```powershell
# Stop all services (preserves volumes)
docker-compose stop

# Stop specific service
docker-compose stop auth-service

# Stop and remove all containers/networks (preserves volumes)
docker-compose down

# Stop and remove everything including volumes
docker-compose down -v

# Restart all services
docker-compose restart

# Restart specific service
docker-compose restart order-service

# Remove containers and networks only
docker-compose rm -f
```

### Interactive Container Access
```powershell
# Get bash shell in running container
docker exec -it mechira-auth-service bash

# Get PowerShell in container
docker exec -it mechira-auth-service powershell

# Run a single command in container
docker exec mechira-auth-service curl http://localhost:5001/health

# Run command with environment variables
docker exec -e CUSTOM_VAR=value mechira-auth-service dotnet --info
```

### Debugging
```powershell
# Inspect container details (IP, mounts, environment)
docker inspect mechira-auth-service

# View container resource usage
docker stats

# View container process list
docker top mechira-auth-service

# Get event stream from Docker daemon
docker events --filter "container=mechira-auth-service"
```

### Database Access
```powershell
# Access SQL Server with sqlcmd
docker exec -it mechira-mssql-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourPasswordHere123!

# Common SQL commands
# Inside sqlcmd:
# GO - Execute statement
# SELECT name FROM sys.databases; GO
# USE Mechira-sinit-microservices; GO
# SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES; GO
```

### Image Management
```powershell
# List all images
docker images

# List only Mechira images
docker images | grep mechira

# Remove unused images
docker image prune

# Remove specific image
docker rmi mechira-auth-service

# Build image without docker-compose
docker build -f Services/AuthService/Dockerfile -t mechira-auth-service:latest .

# Tag image for registry
docker tag mechira-auth-service:latest myregistry.azurecr.io/mechira-auth-service:v1.0
```

### Network Debugging
```powershell
# List all networks
docker network ls

# Inspect Mechira network
docker network inspect mechira_mechira-network

# Test DNS resolution between containers
docker exec mechira-auth-service nslookup order-service

# Test connectivity between services
docker exec mechira-api-gateway curl -v http://auth-service:5001/health

# Check network bandwidth (if supported)
docker exec mechira-auth-service iftop
```

### Service Testing
```powershell
# Health check all services
# Auth Service
curl http://localhost:5001/health

# Catalog Service
curl http://localhost:5002/health

# Order Service
curl http://localhost:5003/health

# Lottery Service
curl http://localhost:5004/health

# API Gateway
curl http://localhost:5000/health
```

### Registry & Cloud
```powershell
# Login to Azure Container Registry
az acr login --name myregistry

# Build and push to Azure
docker build -f Services/AuthService/Dockerfile -t myregistry.azurecr.io/mechira-auth-service:latest .
docker push myregistry.azurecr.io/mechira-auth-service:latest

# Login to Docker Hub
docker login

# Push to Docker Hub
docker tag mechira-auth-service:latest username/mechira-auth-service:latest
docker push username/mechira-auth-service:latest
```

## Service Ports

| Service | Port | URL |
|---------|------|-----|
| API Gateway | 5000 | http://localhost:5000 |
| Auth Service | 5001 | http://localhost:5001 |
| Catalog Service | 5002 | http://localhost:5002 |
| Order Service | 5003 | http://localhost:5003 |
| Lottery Service | 5004 | http://localhost:5004 |
| SQL Server | 1433 | localhost:1433 |

## Environment Variables

### Connection String
```
Server=mssql-db,1433;Database=Mechira-sinit-microservices;User Id=sa;Password=YourPasswordHere123!;TrustServerCertificate=true;Encrypt=false;
```

### Service URLs (Internal Docker DNS)
```
Auth: http://auth-service:5001
Catalog: http://catalog-service:5002
Order: http://order-service:5003
Lottery: http://lottery-service:5004
```

### JWT Configuration
```
SecretKey: your-super-secret-key-that-must-be-at-least-32-characters-long-for-hmac256
Issuer: AuthService
Audience: MicroservicesApi
ExpiryMinutes: 60
```

## Common Scenarios

### Scenario 1: Service Crashed - Restart It
```powershell
docker-compose restart order-service
docker-compose logs -f order-service
```

### Scenario 2: Check Database Connection
```powershell
# From a service container
docker exec mechira-auth-service /opt/mssql-tools/bin/sqlcmd -S mssql-db,1433 -U sa -P YourPasswordHere123! -Q "SELECT 1"

# Or use curl
docker exec mechira-auth-service curl -v mssql-db:1433
```

### Scenario 3: Clear Database and Restart
```powershell
# WARNING: This will delete all data!
docker-compose down -v
docker-compose up --build
```

### Scenario 4: View Service Configuration
```powershell
# See all environment variables for a service
docker exec mechira-auth-service env | sort

# Check which files are in the container
docker exec mechira-auth-service ls -la /app
```

### Scenario 5: Compare Local vs Container
```powershell
# Copy file from container
docker cp mechira-auth-service:/app/appsettings.json ./appsettings.container.json

# Check differences
diff appsettings.json appsettings.container.json
```

## Performance Tuning

### View Resource Usage
```powershell
# Real-time resource usage
docker stats

# Specific container
docker stats mechira-auth-service
```

### Limit Resources
```powershell
# In docker-compose.yml add:
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
```

## Troubleshooting Commands

```powershell
# Check all running processes
ps aux | grep docker

# Force remove all containers
docker rm -f $(docker ps -aq)

# Cleanup all dangling resources
docker system prune

# Full cleanup (WARNING: removes unused images, containers, networks, volumes)
docker system prune -a

# Diagnose Docker issues
docker info

# Check Docker daemon logs (on Windows)
Get-EventLog -LogName Application -Source Docker

# Validate docker-compose.yml syntax
docker-compose config
```

## Best Practices

✅ **Do**:
- Use named volumes for data persistence
- Set resource limits
- Use health checks
- Implement proper logging
- Use environment variables for configuration
- Tag images with version numbers
- Document your Docker setup

❌ **Don't**:
- Run containers as root in production
- Store secrets in Dockerfiles or environment variables (use secrets management)
- Use latest tag in production (use specific versions)
- Leave unused images and containers
- Run without proper logging
- Skip health checks
- Use hardcoded configuration values

## References

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Microsoft .NET Docker Images](https://hub.docker.com/_/microsoft-dotnet)
- [SQL Server Docker Documentation](https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker)
