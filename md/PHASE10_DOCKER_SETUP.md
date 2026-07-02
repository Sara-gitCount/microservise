# PHASE 10: DOCKER SETUP

## Overview
This phase containerizes all microservices and the SQL Server database using Docker and Docker Compose, enabling cloud-ready deployment.

## Files Created

### 1. Dockerfiles
- **Services/AuthService/Dockerfile** - Multi-stage build for AuthService
- **Services/CatalogService/Dockerfile** - Multi-stage build for CatalogService
- **Services/OrderService/Dockerfile** - Multi-stage build for OrderService
- **Services/LotteryService/Dockerfile** - Multi-stage build for LotteryService
- **Gateway/ApiGateway/Dockerfile** - Multi-stage build for API Gateway

Each Dockerfile follows this pattern:
- **Build Stage**: Uses `mcr.microsoft.com/dotnet/sdk:9.0` to compile the application
- **Runtime Stage**: Uses `mcr.microsoft.com/dotnet/aspnet:9.0` for a slim production image
- **Health Check**: Includes `/health` endpoint check
- **Port Exposure**: Exposes service-specific port (5000-5004)

### 2. Docker Compose Configuration
**docker-compose.yml** - Orchestrates all services:
- **mssql-db**: SQL Server 2022 container with shared database
- **auth-service**: AuthService (port 5001)
- **catalog-service**: CatalogService (port 5002)
- **order-service**: OrderService (port 5003)
- **lottery-service**: LotteryService (port 5004)
- **api-gateway**: API Gateway (port 5000)

**Key Features**:
- Single bridge network (`mechira-network`) for internal service-to-service communication
- Database health checks before service startup
- Named volumes for database persistence (`mssql-data`, `mssql-log`, `mssql-backup`)
- Environment variable configuration for all sensitive data
- Restart policies for fault tolerance

### 3. Docker Ignore File
**.dockerignore** - Excludes unnecessary files from Docker builds:
- Version control files (.git, .gitignore)
- Build outputs (bin/, obj/, .vs/)
- Node modules, logs, temporary files
- IDE-specific files

### 4. Environment Configuration
**.env.example** - Template for environment variables
- Copy to `.env` and customize for your environment
- Contains all configurable parameters

## Quick Start

### Prerequisites
- Docker Desktop installed and running
- 8GB RAM minimum recommended
- Ports 5000-5004 and 1433 available

### Step 1: Build and Start Services
```powershell
# Navigate to server directory
cd server

# Start all services
docker-compose up --build

# Start in background
docker-compose up -d --build
```

### Step 2: Verify Services
```powershell
# Check service status
docker-compose ps

# View logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f auth-service
```

### Step 3: Access Services
- **API Gateway**: http://localhost:5000
- **Auth Service**: http://localhost:5001
- **Catalog Service**: http://localhost:5002
- **Order Service**: http://localhost:5003
- **Lottery Service**: http://localhost:5004
- **SQL Server**: localhost:1433 (User: sa, Password: YourPasswordHere123!)

### Step 4: Stop Services
```powershell
# Stop all services
docker-compose down

# Stop and remove volumes (WARNING: deletes database data)
docker-compose down -v
```

## Environment Variables Configuration

### Database Connection
Each service receives the connection string via environment variable:
```
ConnectionStrings__DefaultConnection=Server=mssql-db,1433;Database=Mechira-sinit-microservices;User Id=sa;Password=YourPasswordHere123!;TrustServerCertificate=true;Encrypt=false;
```

**Key Points**:
- Uses internal DNS: `mssql-db` (service name) instead of `localhost`
- Port `1433` is internal Docker port
- `TrustServerCertificate=true` for development environments

### Service URLs (Inter-Service Communication)
Services communicate internally using Docker DNS:
```
ServiceUrls__AuthService=http://auth-service:5001
ServiceUrls__CatalogService=http://catalog-service:5002
ServiceUrls__OrderService=http://order-service:5003
ServiceUrls__LotteryService=http://lottery-service:5004
```

### JWT Configuration
All services receive JWT configuration via environment variables:
```
Jwt__SecretKey=your-super-secret-key-that-must-be-at-least-32-characters-long-for-hmac256
Jwt__Issuer=AuthService
Jwt__Audience=MicroservicesApi
Jwt__ExpiryMinutes=60
```

## Program.cs Configuration

Your services should read configuration from environment variables. Verify this is happening:

```csharp
// In Program.cs - Already implemented in your services
builder.Services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(30)
    )
);
```

The `builder.Configuration` automatically reads from:
1. appsettings.json
2. appsettings.{Environment}.json
3. Environment variables
4. Command line arguments

Environment variables are prefixed with `ConnectionStrings__` which translates to `ConnectionStrings:` in configuration.

## Networking Architecture

```
┌─────────────────────────────────────────┐
│          Host Machine                   │
│                                         │
│  ┌─────────────────────────────────┐   │
│  │   Docker Bridge Network         │   │
│  │  (mechira-network)              │   │
│  │                                 │   │
│  │  ┌───────────────────────────┐  │   │
│  │  │ API Gateway (5000)        │  │   │
│  │  │ ↓ routes to services      │  │   │
│  │  └───────────────────────────┘  │   │
│  │           ↓                      │   │
│  │  ┌─────────────────────────┐    │   │
│  │  │ Auth (5001)             │    │   │
│  │  │ Catalog (5002)          │    │   │
│  │  │ Order (5003)            │    │   │
│  │  │ Lottery (5004)          │    │   │
│  │  └─────────────────────────┘    │   │
│  │           ↓                      │   │
│  │  ┌─────────────────────────┐    │   │
│  │  │ SQL Server (1433)       │    │   │
│  │  └─────────────────────────┘    │   │
│  │                                 │   │
│  └─────────────────────────────────┘   │
│                                         │
│  External Access: localhost:5000-5004  │
└─────────────────────────────────────────┘
```

## Volumes and Persistence

**Database Volumes**:
- `mssql-data`: Stores database files
- `mssql-log`: Stores transaction logs
- `mssql-backup`: Stores backup files

These volumes persist data even when containers are stopped/removed (unless explicitly deleted with `-v` flag).

## Health Checks

All services include health checks:
```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:{PORT}/health || exit 1
```

**Health Check Behavior**:
- Runs every 30 seconds
- Must complete within 10 seconds
- Allows 5 seconds startup time before first check
- Retries 3 times before marking service as unhealthy

## Debugging

### View Container Logs
```powershell
# Follow all logs
docker-compose logs -f

# View last 50 lines
docker-compose logs --tail=50

# View specific service logs
docker-compose logs -f order-service
```

### Access Container Shell
```powershell
# Get shell access to a running container
docker exec -it mechira-auth-service bash
```

### Inspect Network
```powershell
# List all networks
docker network ls

# Inspect the mechira-network
docker network inspect mechira_mechira-network
```

### Test Service Connectivity
```powershell
# From API Gateway container, test connection to AuthService
docker exec mechira-api-gateway curl http://auth-service:5001/health

# From a service, test database connection
docker exec mechira-auth-service curl http://mssql-db:1433
```

## Security Considerations for Production

⚠️ **DO NOT USE IN PRODUCTION**:
1. Change the default SQL Server password in docker-compose.yml
2. Use environment-specific .env files, don't commit to git
3. Implement secrets management (Azure Key Vault, AWS Secrets Manager, etc.)
4. Use HTTPS/TLS for inter-service communication
5. Implement network policies and service mesh (e.g., Istio)
6. Use container registries with authentication
7. Implement proper RBAC and secrets rotation

## Next Steps: Cloud Deployment

### Azure Container Instances (ACI)
```powershell
# Build and push images to Azure Container Registry
az acr build --registry {registryName} -t mechira-api-gateway:latest .
```

### Azure Kubernetes Service (AKS)
```powershell
# Deploy using Helm
helm install mechira ./helm-chart -f values-prod.yaml
```

### Docker Swarm
```powershell
# Initialize swarm mode
docker swarm init

# Deploy stack
docker stack deploy -c docker-compose.prod.yml mechira
```

## Troubleshooting

### Containers failing to start
```powershell
# Check service logs
docker-compose logs

# Rebuild images
docker-compose down
docker-compose up --build
```

### Database connection errors
```powershell
# Verify database container is running
docker-compose ps mssql-db

# Check database logs
docker-compose logs mssql-db
```

### Network issues between services
```powershell
# Verify services can reach each other
docker exec mechira-auth-service ping catalog-service
```

### Port conflicts
```powershell
# Check which process uses the port
netstat -ano | findstr :5001

# Kill the process
taskkill /PID {PID} /F
```

## Summary

✅ **Phase 10 Complete**: Docker infrastructure ready for cloud deployment
- Multi-stage Dockerfiles minimize image size
- Docker Compose orchestrates all services with networking
- Environment variables enable configuration without code changes
- Health checks ensure service reliability
- Volume persistence preserves database state
- Cloud-ready architecture supports Azure, AWS, GCP, and on-premises deployment
