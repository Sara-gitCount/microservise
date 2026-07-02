# Deployment Guide - Mechira Sinit Microservices

## 📋 Table of Contents

1. [Prerequisites](#prerequisites)
2. [Local Development Setup](#local-development-setup)
3. [Docker Build & Run](#docker-build--run)
4. [Docker Compose Production](#docker-compose-production)
5. [Kubernetes Deployment](#kubernetes-deployment)
6. [Environment Configuration](#environment-configuration)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Tools

- **Docker** 20.10+ ([Download](https://www.docker.com/products/docker-desktop))
- **Docker Compose** 1.29+ (included with Docker Desktop)
- **.NET SDK 8.0** ([Download](https://dotnet.microsoft.com/download))
- **SQL Server 2022** (Docker image provided)
- **RabbitMQ 3.13** (Docker image provided)
- **Redis 7** (Docker image provided)
- **Git** (for version control)

### System Requirements

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| RAM | 4 GB | 8 GB |
| CPU | 2 cores | 4 cores |
| Storage | 10 GB | 20 GB |
| OS | Windows 10/11, Linux, macOS | Windows Server 2019+, Ubuntu 20.04+ |

### Check Prerequisites

```powershell
# Check Docker
docker --version
docker-compose --version

# Check .NET
dotnet --version

# Check Git
git --version
```

---

## Local Development Setup

### 1. Clone Repository

```bash
git clone https://github.com/your-org/mechira-microservices.git
cd mechira-microservices/server
```

### 2. Restore NuGet Packages

```bash
# Restore all packages
dotnet restore

# Or specific service
dotnet restore Services/OrderService/OrderService.csproj
```

### 3. Build Solution

```bash
# Build all services (Debug)
dotnet build -c Debug

# Build specific service
dotnet build Services/OrderService/OrderService.csproj -c Debug

# Build for Release
dotnet build -c Release
```

### 4. Run Database Migrations

```bash
# Apply migrations to local SQL Server
# First, ensure SQL Server is running
# Then run migrations from project directory

cd Services/OrderService
dotnet ef database update --configuration Debug

cd ../CatalogService
dotnet ef database update --configuration Debug

# ... repeat for other services
```

### 5. Start Services Locally

**Option A: Run all services with PowerShell script**

```powershell
cd server
powershell -File start-all-services.ps1
```

**Option B: Run individual services**

```powershell
# Terminal 1 - AuthService
cd server/Services/AuthService
dotnet run --configuration Debug

# Terminal 2 - CatalogService
cd server/Services/CatalogService
dotnet run --configuration Debug

# Terminal 3 - OrderService
cd server/Services/OrderService
dotnet run --configuration Debug

# Terminal 4 - LotteryService
cd server/Services/LotteryService
dotnet run --configuration Debug

# Terminal 5 - NotificationService
cd server/Services/NotificationService
dotnet run --configuration Debug
```

**Option C: Run via Docker Compose**

```bash
cd server
docker-compose up -d
```

---

## Docker Build & Run

### Build Docker Images Manually

```bash
cd server

# Build AuthService image
docker build -t mechira/auth-service:1.0 -f Services/AuthService/Dockerfile .

# Build CatalogService image
docker build -t mechira/catalog-service:1.0 -f Services/CatalogService/Dockerfile .

# Build OrderService image
docker build -t mechira/order-service:1.0 -f Services/OrderService/Dockerfile .

# Build LotteryService image
docker build -t mechira/lottery-service:1.0 -f Services/LotteryService/Dockerfile .

# Build NotificationService image
docker build -t mechira/notification-service:1.0 -f Services/NotificationService/Dockerfile .

# Build API Gateway image
docker build -t mechira/api-gateway:1.0 -f Gateway/ApiGateway/Dockerfile .
```

### Run Individual Container

```bash
# Run AuthService container
docker run -d \
  --name auth-service \
  -p 5001:5001 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal,1433;Database=Mechira-sinit-microservices;User Id=sa;Password=YourPasswordHere123!" \
  -e Jwt__SecretKey="your-super-secret-key-that-must-be-at-least-32-characters-long-for-hmac256" \
  --network mechira-network \
  mechira/auth-service:1.0
```

### View Container Logs

```bash
# View logs for a container
docker logs auth-service

# Stream logs in real-time
docker logs -f auth-service

# Last 100 lines
docker logs --tail 100 auth-service
```

### Stop Container

```bash
docker stop auth-service
docker rm auth-service
```

---

## Docker Compose Production

### Start All Services

```bash
cd server

# Start all services in detached mode
docker-compose up -d

# Or with verbose output
docker-compose up

# Start specific services
docker-compose up -d mssql-db rabbitmq-service redis-cache
docker-compose up -d auth-service catalog-service order-service
```

### Check Service Status

```bash
# List all running containers
docker-compose ps

# Example output:
# NAME                        STATUS
# mechira-mssql-db           Up 2 minutes (healthy)
# mechira-auth-service       Up 1 minute
# mechira-catalog-service    Up 50 seconds
# mechira-order-service      Up 40 seconds
# mechira-notification-service Up 35 seconds
# mechira-api-gateway        Up 30 seconds
```

### View Logs

```bash
# All services
docker-compose logs

# Specific service
docker-compose logs auth-service

# Real-time stream
docker-compose logs -f order-service

# Last 50 lines
docker-compose logs --tail 50 catalog-service
```

### Restart Services

```bash
# Restart specific service
docker-compose restart auth-service

# Restart all services
docker-compose restart

# Force recreate containers
docker-compose up -d --force-recreate
```

### Stop Services

```bash
# Stop all services (containers preserved)
docker-compose stop

# Stop specific service
docker-compose stop auth-service

# Remove all containers (data persists in volumes)
docker-compose down

# Remove everything including volumes
docker-compose down -v
```

---

## Kubernetes Deployment

### Prerequisites

- **Kubernetes Cluster** (1.20+)
- **kubectl** CLI
- **Docker images** pushed to registry (Docker Hub, ECR, ACR)

### Create Namespace

```bash
kubectl create namespace mechira-microservices
kubectl config set-context --current --namespace=mechira-microservices
```

### Create ConfigMap (Configuration)

```yaml
# config.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: microservices-config
  namespace: mechira-microservices
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  ServiceUrls__AuthService: "http://auth-service:5001"
  ServiceUrls__CatalogService: "http://catalog-service:5002"
  ServiceUrls__OrderService: "http://order-service:5003"
  ServiceUrls__LotteryService: "http://lottery-service:5004"
```

### Create Secret (Sensitive Data)

```bash
kubectl create secret generic microservices-secrets \
  --from-literal=Jwt__SecretKey="your-super-secret-key-that-must-be-at-least-32-characters-long-for-hmac256" \
  --from-literal=ConnectionStrings__DefaultConnection="Server=mssql-db.mechira-microservices.svc.cluster.local,1433;Database=Mechira-sinit-microservices;User Id=sa;Password=YourPasswordHere123!;TrustServerCertificate=true;Encrypt=false;" \
  -n mechira-microservices
```

### Deploy SQL Server

```yaml
# mssql-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mssql-db
  namespace: mechira-microservices
spec:
  replicas: 1
  selector:
    matchLabels:
      app: mssql-db
  template:
    metadata:
      labels:
        app: mssql-db
    spec:
      containers:
      - name: mssql
        image: mcr.microsoft.com/mssql/server:2022-latest
        ports:
        - containerPort: 1433
        env:
        - name: ACCEPT_EULA
          value: "Y"
        - name: MSSQL_SA_PASSWORD
          valueFrom:
            secretKeyRef:
              name: microservices-secrets
              key: MSSQL_PASSWORD
        resources:
          requests:
            memory: "2Gi"
            cpu: "1000m"
          limits:
            memory: "4Gi"
            cpu: "2000m"
---
apiVersion: v1
kind: Service
metadata:
  name: mssql-db
  namespace: mechira-microservices
spec:
  selector:
    app: mssql-db
  ports:
  - port: 1433
    targetPort: 1433
  type: ClusterIP
```

### Deploy Service Example (OrderService)

```yaml
# order-service-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: order-service
  namespace: mechira-microservices
spec:
  replicas: 2
  selector:
    matchLabels:
      app: order-service
  template:
    metadata:
      labels:
        app: order-service
    spec:
      containers:
      - name: order-service
        image: your-registry/mechira/order-service:1.0
        ports:
        - containerPort: 5003
        envFrom:
        - configMapRef:
            name: microservices-config
        - secretRef:
            name: microservices-secrets
        env:
        - name: ASPNETCORE_URLS
          value: "http://+:5003"
        livenessProbe:
          httpGet:
            path: /api/health/live
            port: 5003
          initialDelaySeconds: 10
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /api/health/ready
            port: 5003
          initialDelaySeconds: 5
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 2
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
---
apiVersion: v1
kind: Service
metadata:
  name: order-service
  namespace: mechira-microservices
spec:
  selector:
    app: order-service
  ports:
  - port: 5003
    targetPort: 5003
  type: ClusterIP
---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: order-service-hpa
  namespace: mechira-microservices
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: order-service
  minReplicas: 2
  maxReplicas: 5
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

### Deploy All Services

```bash
# Apply configurations and secrets
kubectl apply -f config.yaml
kubectl apply -f secrets.yaml

# Deploy infrastructure (DB, Cache, Message Broker)
kubectl apply -f mssql-deployment.yaml
kubectl apply -f redis-deployment.yaml
kubectl apply -f rabbitmq-deployment.yaml

# Wait for infrastructure to be ready
kubectl wait --for=condition=ready pod -l app=mssql-db --timeout=300s

# Deploy services
kubectl apply -f auth-service-deployment.yaml
kubectl apply -f catalog-service-deployment.yaml
kubectl apply -f order-service-deployment.yaml
kubectl apply -f lottery-service-deployment.yaml
kubectl apply -f notification-service-deployment.yaml
kubectl apply -f api-gateway-deployment.yaml

# Deploy Ingress for external access
kubectl apply -f ingress.yaml
```

### Monitor Deployment

```bash
# Watch deployments
kubectl get deployments -w -n mechira-microservices

# Check pods
kubectl get pods -n mechira-microservices

# Describe pod for details
kubectl describe pod order-service-xyz123 -n mechira-microservices

# Check service status
kubectl get svc -n mechira-microservices
```

---

## Environment Configuration

### Configuration Files

| Service | Location | Purpose |
|---------|----------|---------|
| OrderService | `appsettings.json` | Base configuration |
| OrderService | `appsettings.Development.json` | Dev-specific overrides |
| OrderService | `appsettings.Production.json` | Prod-specific overrides |

### Example appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=Mechira-sinit-microservices;User Id=sa;Password=YourPasswordHere123!;TrustServerCertificate=true;Encrypt=false;"
  },
  "Jwt": {
    "SecretKey": "your-super-secret-key-that-must-be-at-least-32-characters-long-for-hmac256",
    "Issuer": "AuthService",
    "Audience": "MicroservicesApi",
    "ExpiryMinutes": 60
  },
  "Services": {
    "AuthService": {
      "Url": "http://localhost:5001"
    },
    "CatalogService": {
      "Url": "http://localhost:5002"
    },
    "OrderService": {
      "Url": "http://localhost:5003"
    },
    "LotteryService": {
      "Url": "http://localhost:5004"
    }
  },
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest"
  },
  "Redis": {
    "Host": "localhost",
    "Port": 6379
  }
}
```

### Environment Variables (Docker)

```bash
# Database
ConnectionStrings__DefaultConnection="Server=mssql-db,1433;Database=Mechira-sinit-microservices;User Id=sa;Password=YourPasswordHere123!;TrustServerCertificate=true;Encrypt=false;"

# JWT
Jwt__SecretKey="your-super-secret-key-that-must-be-at-least-32-characters-long-for-hmac256"
Jwt__Issuer="AuthService"
Jwt__Audience="MicroservicesApi"
Jwt__ExpiryMinutes="60"

# Service URLs
ServiceUrls__AuthService="http://auth-service:5001"
ServiceUrls__CatalogService="http://catalog-service:5002"
ServiceUrls__OrderService="http://order-service:5003"
ServiceUrls__LotteryService="http://lottery-service:5004"

# RabbitMQ
RABBITMQ_HOST="rabbitmq-service"
RABBITMQ_PORT="5672"
RABBITMQ_USERNAME="guest"
RABBITMQ_PASSWORD="guest"

# Redis
REDIS_HOST="redis-cache"
REDIS_PORT="6379"

# ASP.NET Core
ASPNETCORE_ENVIRONMENT="Production"
ASPNETCORE_URLS="http://+:5003"
ASPNETCORE_HTTPS_PORT="5003"
```

---

## Health Checks & Readiness

### Kubernetes Health Check Configuration

```yaml
livenessProbe:
  httpGet:
    path: /api/health/live
    port: 5003
  initialDelaySeconds: 10
  periodSeconds: 10
  timeoutSeconds: 5
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /api/health/ready
    port: 5003
  initialDelaySeconds: 5
  periodSeconds: 5
  timeoutSeconds: 3
  failureThreshold: 2
```

### Manual Health Check

```bash
# Test health endpoint
curl http://localhost:5003/api/health

# Test liveness
curl http://localhost:5003/api/health/live

# Test readiness
curl http://localhost:5003/api/health/ready
```

---

## Troubleshooting

### Service Won't Start

```bash
# Check logs for errors
docker-compose logs order-service | grep -i error

# Common issues:
# 1. Port already in use
netstat -ano | findstr :5003

# 2. Database connection failed
# Verify connection string, SQL Server running
# 3. RabbitMQ connection failed
# Verify RabbitMQ running on port 5672
```

### Database Connection Issues

```bash
# Check if SQL Server is running
docker-compose ps mssql-db

# Test connection
sqlcmd -S localhost,1433 -U sa -P YourPasswordHere123! -Q "SELECT 1"

# Check logs
docker-compose logs mssql-db | tail -50
```

### Message Broker Issues

```bash
# Check if RabbitMQ is running
docker-compose ps rabbitmq-service

# Access RabbitMQ Management UI
# http://localhost:15672
# Default credentials: guest/guest

# Check logs
docker-compose logs rabbitmq-service | tail -50
```

### Cache Issues

```bash
# Check if Redis is running
docker-compose ps redis-cache

# Connect to Redis CLI
docker-compose exec redis-cache redis-cli

# In Redis CLI:
# PING              - Test connection
# KEYS *            - List all keys
# GET product:1     - Get specific key
# FLUSHDB           - Clear cache
```

### Container Crashes

```bash
# Check exit code
docker-compose ps

# View detailed logs
docker-compose logs --tail 200 order-service

# Inspect container
docker-compose exec order-service sh

# Common exit codes:
# 0 = Normal shutdown
# 1 = Application error
# 126 = Cannot execute binary
# 137 = Out of memory
```

---

## Performance Optimization

### Docker Compose Scale

```bash
# Scale OrderService to 3 instances
docker-compose up -d --scale order-service=3

# Note: Requires load balancer frontend (Traefik, nginx)
```

### Resource Limits

```yaml
# Set resource limits in docker-compose
services:
  order-service:
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 1G
        reservations:
          cpus: '0.5'
          memory: 512M
```

---

## Backup & Restore

### Database Backup

```bash
# Backup SQL Server database
docker-compose exec mssql-db \
  /opt/mssql-tools/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P YourPasswordHere123! \
  -Q "BACKUP DATABASE [Mechira-sinit-microservices] TO DISK = '/var/opt/mssql/backup/db-backup.bak'"
```

### Volume Backup

```bash
# Backup volumes
docker run --rm \
  -v mechira-microservices_mssql-data:/data \
  -v /backup:/backup \
  ubuntu \
  tar czf /backup/mssql-data.tar.gz -C /data .
```

---

## Production Checklist

- [ ] All services pass health checks
- [ ] Database backups configured
- [ ] Logs aggregated and monitored
- [ ] SSL/TLS certificates installed
- [ ] Rate limiting enabled
- [ ] Security scanning completed
- [ ] Performance testing done
- [ ] Disaster recovery plan documented
- [ ] On-call monitoring setup
- [ ] Runbooks created for common issues

---

**Version:** 1.0  
**Last Updated:** 2026-07-02  
**Maintained by:** DevOps Team
