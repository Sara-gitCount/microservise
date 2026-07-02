# ADR-001: Database Isolation Pattern

## Status
✅ **ACCEPTED**

## Date
2026-07-02

## Context
The microservices architecture requires independent deployment, scaling, and data management for each service. Previously, all 5 services (AuthService, CatalogService, OrderService, LotteryService, NotificationService) shared a single `Mechira-sinit-microservices` SQL Server database.

This violates the microservices principle that each service should own its data and be independently deployable. Shared databases create:
- **Tight coupling:** Services depend on shared schema
- **Scaling bottlenecks:** Database becomes a single point of contention
- **Deployment risk:** One service's migration failure blocks all services

## Decision
**Implement database-per-service using SQL Server isolated databases.**

- Each microservice has its own SQL Server database
- Databases: `Mechira-AuthService`, `Mechira-CatalogService`, `Mechira-OrderService`, `Mechira-LotteryService`, `Mechira-NotificationService`
- Single SQL Server instance (port 1433), multiple databases within it
- Each service's EF Core migrations create/update its schema independently
- Services are deployed to separate databases at container start via `MigrateAsync()`

## Rationale

### 1. **Microservices Autonomy** ✅
Each service can:
- Deploy independently without impacting others
- Scale database resources independently
- Upgrade SQL Server version independently
- Choose different backup/recovery strategies

### 2. **Consistency Models** ✅
Different services have different consistency needs:
- **Orders:** Require ACID (financial data) → SQL with transactions
- **Catalog:** Eventual consistency acceptable (product data changes rarely) → SQL is fine
- **Auth:** Strong consistency required → SQL transactions

### 3. **Operational Simplicity** ✅
- Single SQL Server instance reduces ops overhead vs. polyglot persistence
- Easier to monitor, backup, and manage all DBs in one place
- Team already familiar with SQL Server

### 4. **CAP Theorem Application** ✅
Our system chooses **Consistency over Availability** (CP model):
- OrderService: Prioritizes consistency (money transactions must be accurate)
- CatalogService: Accepts eventual consistency (cache with TTL is fine)
- Multi-service consistency: Via saga pattern (Phase 4, already implemented)

## Consequences

### Positive ✅
- **True Microservices:** Services can be developed, tested, and deployed independently
- **No Shared Schema Risk:** Breaking schema changes don't ripple across services
- **Clear Boundaries:** Each service's tables are in its database, not mixed with others
- **Independent Backups:** Each service can have different backup retention policies

### Negative ⚠️
- **No Distributed Transactions:** Can't use `BEGIN TRANSACTION ... COMMIT` across services
  - **Mitigation:** Saga pattern for compensating transactions (already implemented)
- **No JOIN Across Services:** Can't write queries that JOIN across databases
  - **Mitigation:** BFF (Backend-for-Frontend) layer aggregates data via HTTP (Phase 3)
- **Data Duplication:** Services may cache/duplicate reference data
  - **Mitigation:** Redis cache for frequently-read data (Phase 4, already implemented)

## Alternatives Considered

### 1. ❌ Shared Single Database
- **Pros:** Simpler initially, ACID across all data
- **Cons:** Violates microservices isolation, prevents independent scaling/deployment
- **Status:** Rejected (was the initial state; moving away from it)

### 2. ❌ Polyglot Persistence (NoSQL + SQL)
- **Pros:** SQL for Orders (ACID), MongoDB for Catalog (document model)
- **Cons:** Operational complexity, ops team must manage multiple DB types
- **Status:** Rejected for Phase 1 (user chose to keep all SQL); can revisit in bonus phase

### 3. ✅ **Database-Per-Service (CHOSEN)**
- Single SQL Server instance
- Separate database per service
- EF Core migrations managed per service

## Implementation

### Files Modified
- `docker-compose.yml` — Updated connection strings, added init script
- `appsettings.json` (per service) — Database names updated
- `Program.cs` (per service) — Added `dbContext.Database.MigrateAsync()` on startup
- `init-databases.sql` — SQL script to initialize all 5 databases

### Verification
```bash
# All 5 databases should exist in SQL Server:
SELECT name FROM sys.databases WHERE name LIKE 'Mechira-%'

# Each service should log successful migrations:
docker compose logs | grep "migrations completed"
```

## Related Decisions
- **ADR-002:** Database technology choices (why SQL Server for all services in Phase 1)
- **Phase 2:** Correlation IDs trace requests across isolated databases
- **Phase 4:** Saga pattern handles multi-service transactions (already implemented)
- **Phase 3:** BFF layer aggregates data from multiple services (planned)

## References
- Microservices Patterns: Database per service (Sam Newman, Chapter 3)
- CAP Theorem: Consistency, Availability, Partition tolerance
- EF Core Migrations: `context.Database.MigrateAsync()`
