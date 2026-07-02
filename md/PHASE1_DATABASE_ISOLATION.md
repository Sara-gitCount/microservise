# Phase 1: Database Isolation Implementation

## Overview
Phase 1 implements the **database-per-service** architecture pattern, a foundational requirement of the microservices specification. Instead of all services sharing a single `Mechira-sinit-microservices` database, each service now has its own isolated SQL Server database.

## Changes Made

### 1. Database Separation

Each service now has a dedicated database:

| Service | Database Name | Schema | Entities |
|---------|---------------|--------|----------|
| **AuthService** | `Mechira-AuthService` | `Auth` | Users, authentication tokens |
| **CatalogService** | `Mechira-CatalogService` | `Catalog` | Donors, Categories, Gifts |
| **OrderService** | `Mechira-OrderService` | `Orders` | Orders, order tracking |
| **LotteryService** | `Mechira-LotteryService` | `Lottery` | Lottery draws, participants |
| **NotificationService** | `Mechira-NotificationService` | `Notifications` | Event-driven (optional DB for audit logs) |

**SQL Server Instance:** Single MSSQL instance (port 1433), multiple databases within it.

### 2. Configuration Files Updated

#### `docker-compose.yml`
- Updated all service connection strings to use service-specific databases
- Example (AuthService):
  ```yaml
  ConnectionStrings__DefaultConnection: "Server=mssql-db,1433;Database=Mechira-AuthService;User Id=sa;Password=YourPasswordHere123!;..."
  ```

#### `appsettings.json` (per service)
- Updated local development connection strings for each service
- Example (CatalogService):
  ```json
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\MSSQLSERVER01;Database=Mechira-CatalogService;..."
  }
  ```

#### `init-databases.sql`
- Created SQL initialization script (at `server/init-databases.sql`)
- Ensures all 5 databases exist when SQL Server container starts
- Runs automatically via docker-compose volume mount

### 3. Automatic Migration on Startup

Added database migration execution to each service's `Program.cs`:

```csharp
// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<{ServiceName}DbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying database migrations for {ServiceName}...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations completed for {ServiceName}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error applying database migrations for {ServiceName}");
        throw;
    }
}
```

**Services Updated:**
- `Services/AuthService/Program.cs`
- `Services/CatalogService/Program.cs`
- `Services/OrderService/Program.cs`
- `Services/LotteryService/Program.cs`

### 4. Why This Approach?

**Benefits of Database-Per-Service:**
- ✅ **Data Isolation:** Each service owns its data; no cross-service direct database access
- ✅ **Independent Scaling:** Services can have different backup/replication strategies
- ✅ **Technology Choice:** Services can upgrade SQL Server independently
- ✅ **Deployment Safety:** Failures in one service's migrations don't affect others
- ✅ **Compliance with Spec:** Mandatory Phase 2 requirement: "database-per-service"

**Trade-offs:**
- ⚠️ **Distributed Transactions:** No longer available for ACID guarantees across services
  - *Solution:* Use compensating transactions (saga pattern) for multi-service flows — already implemented with RabbitMQ choreography in Phase 4
- ⚠️ **Data Consistency Queries:** Can't JOIN across databases
  - *Solution:* Use BFF (Phase 3) to aggregate data from multiple services via HTTP calls

## Verification Checklist

### ✅ Verify Docker Setup
```bash
# Start the services
docker compose up -d

# Verify SQL Server is healthy
docker compose ps

# Check that all databases were created (connect to SQL Server):
# - Mechira-AuthService
# - Mechira-CatalogService
# - Mechira-OrderService
# - Mechira-LotteryService
# - Mechira-NotificationService
```

### ✅ Verify Each Service Connects Independently
```bash
# Check logs to see migration messages
docker compose logs auth-service | grep -i "migration"
docker compose logs catalog-service | grep -i "migration"
docker compose logs order-service | grep -i "migration"
docker compose logs lottery-service | grep -i "migration"
```

### ✅ Verify Data Isolation
```bash
# In SQL Server Management Studio, verify:
# 1. Each database contains only its service's schema
# 2. AuthService DB has only [Auth].[Users] table
# 3. CatalogService DB has [Catalog].[Donors/Categories/Gifts]
# 4. OrderService DB has [Orders].[Orders]
# 5. LotteryService DB has its tables
```

## Architecture Decision Record (ADR)

### ADR-001: Database-Per-Service Pattern

**Status:** ✅ Accepted  
**Date:** 2026-07-02  
**Context:** Microservices architecture requires data isolation for independent deployment and scaling.  

**Decision:**
- Implement database-per-service using SQL Server with isolated databases
- Each service owns its database and schema
- No direct cross-service database access

**Rationale:**
1. **Microservices Principle:** Each service must be independently deployable and scalable
2. **Data Consistency:** Different services have different consistency needs (Orders = ACID, Catalog = eventual)
3. **Operational Simplicity:** Single MSSQL instance reduces ops overhead vs. multiple database types
4. **No Polyglot Yet:** All services use SQL Server (could be extended to MongoDB, Redis, etc. in future phases)

**Consequences:**
- ✅ Services are truly independent
- ❌ Multi-service transactions require sagas (already implemented via RabbitMQ)
- ❌ Complex queries need BFF aggregation layer (planned for Phase 3)

**Alternatives Considered:**
1. **Shared Database:** Violates microservices principle, would need this phase
2. **Polyglot (MongoDB for Catalog):** Would add complexity; kept simple for now

## Files Modified

| File | Change |
|------|--------|
| `server/docker-compose.yml` | Updated all service connection strings to use separate databases |
| `server/Services/AuthService/appsettings.json` | Changed database to `Mechira-AuthService` |
| `server/Services/CatalogService/appsettings.json` | Changed database to `Mechira-CatalogService` |
| `server/Services/OrderService/appsettings.json` | Changed database to `Mechira-OrderService` |
| `server/Services/LotteryService/appsettings.json` | Changed database to `Mechira-LotteryService` |
| `server/Services/AuthService/Program.cs` | Added migration code on startup |
| `server/Services/CatalogService/Program.cs` | Added migration code on startup |
| `server/Services/OrderService/Program.cs` | Added migration code on startup |
| `server/Services/LotteryService/Program.cs` | Added migration code on startup |
| `server/init-databases.sql` | **NEW** — Database initialization script |

## Next Steps

1. **Test locally:** Run `docker compose up` and verify all services start and migrations run
2. **Phase 2:** Add Correlation ID propagation (observability)
3. **Phase 3:** Add Loki log aggregation (centralized logging)
4. **Phase 4:** Add BFF layer (client aggregation)
5. **Phase 5:** Add load balancing (scalability proof)

## Links to Related Documentation

- [Phase 2: Correlation ID Propagation](./PHASE2_CORRELATION_ID.md) — Trace requests across services
- [Phase 4: Messaging & Saga](./PHASE4_MESSAGING_SAGA.md) — Already implemented (RabbitMQ)
- [ADR-002: Database Choices](./ADR_002_DATABASE_CHOICES.md) — Why SQL for orders, polyglot future
