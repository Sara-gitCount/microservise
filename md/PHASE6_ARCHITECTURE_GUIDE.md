# Mechira Sinit Microservices - Architecture Guide

## 📋 Table of Contents

1. [System Overview](#system-overview)
2. [Service Architecture](#service-architecture)
3. [Event-Driven Architecture](#event-driven-architecture)
4. [Database Design](#database-design)
5. [Caching Strategy](#caching-strategy)
6. [Communication Patterns](#communication-patterns)
7. [Resilience Patterns](#resilience-patterns)

---

## System Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     API Gateway (5000)                       │
│              (Ocelot - API Routing & Rate Limiting)          │
└──────────┬──────────┬──────────┬──────────┬──────────────────┘
           │          │          │          │
    ┌──────▼─┐  ┌────▼──┐  ┌───▼───┐  ┌──▼─────┐
    │ Auth   │  │Catalog│  │Order  │  │Lottery │
    │Service │  │Service│  │Service│  │Service │
    │ :5001  │  │ :5002 │  │ :5003 │  │ :5004  │
    └──┬──┬──┘  └──┬────┘  └───┬───┘  └────────┘
       │  │        │          │
       │  │    ┌───┴──────────┴────┐
       │  │    │ Notification     │
       │  │    │ Service :5005    │
       │  │    │ (Event-Driven)   │
       │  │    └───┬──────────────┘
       │  │        │
    ┌──▼──▼────────▼────┐
    │   Message Broker   │
    │  (RabbitMQ 5672)   │
    │  (Management 15672)│
    └────────────────────┘
         │      │
    ┌────▼──────▼───────────────────┐
    │  Distributed Cache (Redis)    │
    │  (Port 6379)                   │
    │  TTL: 10 minutes               │
    └────────────────────────────────┘
         │
    ┌────▼──────────────────────────┐
    │   SQL Server Database 2022     │
    │   (Port 1433)                  │
    │   Shared Single Database       │
    │   Schema-separated Services    │
    └───────────────────────────────┘
```

### Architecture Layers

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **API Gateway** | Ocelot (C#/.NET 8) | Request routing, rate limiting, load balancing |
| **Services** | .NET 8 (C#) | Business logic, independent deployability |
| **Message Broker** | RabbitMQ 3.13 | Asynchronous event communication |
| **Cache** | Redis 7 | Distributed caching (cache-aside pattern) |
| **Database** | SQL Server 2022 | Persistent data storage |
| **Logging** | Serilog 8.0.0 | Structured logging with correlation IDs |
| **Auth** | JWT (HS256) | Service-to-service authentication |

---

## Service Architecture

### 1. **AuthService** (Port 5001)
**Responsibility:** User authentication & authorization

**Endpoints:**
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login (returns JWT)
- `POST /api/auth/refresh` - Token refresh
- `GET /api/health` - Health check
- `GET /api/health/live` - Liveness probe
- `GET /api/health/ready` - Readiness probe

**Dependencies:**
- SQL Server (default database)
- No external service calls

**Tech Stack:**
- Entity Framework Core 8.0
- BCrypt.Net-Next 4.2.0 (password hashing)
- JWT 7.0.3

---

### 2. **CatalogService** (Port 5002)
**Responsibility:** Product/Gift management & inventory

**Endpoints:**
- `GET /api/products` - List all products
- `GET /api/products/{id}` - Get product by ID (cached)
- `POST /api/products` - Create new product
- `PUT /api/products/{id}` - Update product
- `DELETE /api/products/{id}` - Delete product
- `GET /api/health` - Health check (includes cache status)

**Event Consumption:**
- `OrderPlaced` - Validates inventory, publishes `InventoryReserved` or `InventoryFailed`

**Dependencies:**
- SQL Server (schema: `Catalog`)
- Redis (distributed cache)
- RabbitMQ (event consumption)

**Tech Stack:**
- StackExchange.Redis 2.7.0
- MassTransit 8.2.0
- Entity Framework Core 8.0

**Caching Strategy (Cache-Aside Pattern):**
```
GET /api/products/1
  ├─ Check Redis: key = "product:1"
  ├─ IF found (CacheHit) → Return cached value
  ├─ IF not found (CacheMiss)
  │  ├─ Query Database
  │  ├─ Set Redis with TTL 10 minutes
  │  └─ Return value
  └─ ON Redis error → Fallback to DB (fail gracefully)
```

---

### 3. **OrderService** (Port 5003)
**Responsibility:** Order management with Saga coordination

**Endpoints:**
- `GET /api/orders` - List user orders
- `POST /api/orders` - Create new order (saga initiation)
- `GET /api/orders/{id}` - Get order by ID
- `PUT /api/orders/{id}` - Update order status
- `GET /api/health` - Health check

**Event Publishing:**
- `OrderPlaced` - Initiates Saga

**Event Consumption:**
- `InventoryReserved` - Updates order to "Confirmed", publishes `OrderConfirmed`
- `InventoryFailed` - Updates order to "Cancelled", publishes `OrderCancelled`

**Dependencies:**
- SQL Server (schema: `Orders`)
- RabbitMQ (event pub/sub)
- AuthService (HTTP with resilience)
- CatalogService (HTTP with resilience)

**Tech Stack:**
- Polly 8.3.1 (retry + circuit breaker)
- MassTransit 8.2.0
- Entity Framework Core 8.0

---

### 4. **LotteryService** (Port 5004)
**Responsibility:** Lottery/raffle management

**Endpoints:**
- `GET /api/lotteries` - List lotteries
- `POST /api/lotteries` - Create lottery
- `GET /api/lotteries/{id}` - Get lottery by ID
- `GET /api/health` - Health check

**Dependencies:**
- SQL Server (schema: `Lottery`)

**Tech Stack:**
- Entity Framework Core 8.0

---

### 5. **NotificationService** (Port 5005) - **NEW (Phase 4)**
**Responsibility:** Event-driven email notifications

**Event Consumption:**
- `OrderConfirmed` - Sends confirmation email
- `OrderCancelled` - Sends cancellation email

**Features:**
- Asynchronous email simulation (100ms delay)
- Non-blocking consumption (failures don't stop saga)
- Structured logging

**Dependencies:**
- RabbitMQ (event consumption only, no database)

**Tech Stack:**
- MassTransit 8.2.0
- Serilog 8.0.0

---

## Event-Driven Architecture

### Order Saga Pattern (Event Choreography)

#### Happy Path Flow:

```
1. POST /api/orders (OrderService)
   │
   ├─ Save order to DB (status: "Pending")
   └─ PUBLISH: OrderPlaced {OrderId, UserId, GiftId, Quantity, TotalPrice}
      │
      ├─> CatalogService receives OrderPlaced
      │   ├─ Query product (check cache first via Redis)
      │   ├─ Verify quantity in stock
      │   ├─ IF sufficient:
      │   │  ├─ Decrement inventory
      │   │  ├─ Save to DB
      │   │  └─ PUBLISH: InventoryReserved {OrderId, ReservedItems}
      │   │     │
      │   │     └─> OrderService receives InventoryReserved
      │   │         ├─ Update order status to "Confirmed"
      │   │         ├─ Save to DB
      │   │         └─ PUBLISH: OrderConfirmed {OrderId, UserId, ConfirmedAt}
      │   │            │
      │   │            └─> NotificationService receives OrderConfirmed
      │   │                ├─ Simulate email send (100ms)
      │   │                └─ LOG: "Confirmation email sent"
      │   │
      │   └─ IF insufficient:
      │      └─ PUBLISH: InventoryFailed {OrderId, Reason: "Out of stock"}
      │         │
      │         └─> OrderService receives InventoryFailed
      │             ├─ Update order status to "Cancelled"
      │             ├─ Save to DB
      │             └─ PUBLISH: OrderCancelled {OrderId, UserId, Reason}
      │                │
      │                └─> NotificationService receives OrderCancelled
      │                    ├─ Simulate email send (100ms)
      │                    └─ LOG: "Cancellation email sent"
      │
      └─ RESPONSE: 201 Created {OrderId, Status: "Pending"}
```

### Event Definitions

**OrderPlaced**
```csharp
{
    OrderId: Guid,
    UserId: Guid,
    GiftId: int,
    Quantity: int,
    TotalPrice: decimal,
    CreatedAt: DateTime,
    Items: List<OrderItemDto>
}
```

**InventoryReserved**
```csharp
{
    OrderId: Guid,
    ReservedAt: DateTime,
    ReservedItems: List<ReservedItemDto>
}
```

**InventoryFailed**
```csharp
{
    OrderId: Guid,
    FailedAt: DateTime,
    Reason: string
}
```

**OrderConfirmed**
```csharp
{
    OrderId: Guid,
    UserId: Guid,
    ConfirmedAt: DateTime
}
```

**OrderCancelled**
```csharp
{
    OrderId: Guid,
    UserId: Guid,
    CancelledAt: DateTime,
    Reason: string
}
```

---

## Database Design

### Shared Single Database
- **Database Name:** `Mechira-sinit-microservices`
- **Server:** SQL Server 2022
- **Schema Separation:** Each service has its own schema

### Schemas

```
Database: Mechira-sinit-microservices
├── Auth (AuthService)
│   ├── Users
│   ├── Roles
│   └── UserRoles
├── Catalog (CatalogService)
│   ├── Products
│   ├── Categories
│   └── Inventory
├── Orders (OrderService)
│   ├── Orders
│   ├── OrderItems
│   └── OrderStatus
├── Lottery (LotteryService)
│   ├── Lotteries
│   ├── LotteryTickets
│   └── Winners
└── Audit (Shared)
    └── AuditLogs
```

---

## Caching Strategy

### Redis Configuration
- **Port:** 6379
- **Image:** redis:7-alpine
- **Persistence:** Volume-mounted (redis-data)
- **Pattern:** Cache-Aside (Lazy Loading)

### Cache Keys

| Service | Key Pattern | TTL | Purpose |
|---------|-------------|-----|---------|
| CatalogService | `product:{id}` | 10 min | Product details |
| CatalogService | `products:all` | 10 min | Product list |
| CatalogService | `category:{id}` | 10 min | Category details |

### Cache Invalidation

| Event | Action |
|-------|--------|
| Product updated | `DEL product:{id}` |
| Product deleted | `DEL product:{id}` |
| Inventory changed | `DEL product:{id}` |
| Bulk update | `FLUSHDB` or pattern match |

---

## Communication Patterns

### Synchronous (HTTP with Resilience)

**OrderService → AuthService / CatalogService**

```csharp
// Polly Resilience Policies
├── Retry Policy
│   ├── Max Attempts: 3
│   ├── Backoff: Exponential (2s base)
│   └── On Conditions: Timeout, 5xx errors
│
└── Circuit Breaker
    ├── Failure Ratio: 50%
    ├── Min Throughput: 5 requests
    ├── Sample Duration: 30s
    └── Break Duration: 30s
```

**Failure Handling:**
- On transient failure → Retry with exponential backoff
- On persistent failure → Circuit breaker opens
- Circuit state: Open → Half-Open → Closed
- Fallback: Return cached response or 503 Service Unavailable

### Asynchronous (Event-Driven via RabbitMQ)

**RabbitMQ Configuration**
- **Exchange:** Fanout (topic-based)
- **Queues:** Per consumer
- **Message Serialization:** JSON
- **Acknowledgment:** Auto-ack after successful processing
- **Dead Letter Queue:** For failed messages

---

## Resilience Patterns

### 1. **Retry Pattern (Polly)**
- **Transient Failures:** Network timeout, HTTP 5xx
- **Strategy:** Exponential backoff (2s, 4s, 8s)
- **Max Attempts:** 3

### 2. **Circuit Breaker Pattern (Polly)**
- **Purpose:** Prevent cascading failures
- **States:**
  - Closed → Normal operation
  - Open → Failing, requests rejected
  - Half-Open → Testing recovery
- **Trigger:** 50% failure ratio over 5+ requests

### 3. **Timeout Pattern**
- **HTTP Timeout:** 5 seconds per request
- **Database Timeout:** 30 seconds per command
- **Message Processing:** 30 seconds max

### 4. **Graceful Degradation**
- **Cache Failures:** Fall back to database
- **Email Send Failures:** Log and continue (don't break saga)
- **Database Unavailable:** Return 503 Service Unavailable

### 5. **Health Checks**
- **Liveness (`/health/live`):** Service is running
- **Readiness (`/health/ready`):** Service can accept requests
- **Startup Checks:** All dependencies initialized

---

## Correlation & Observability

### Correlation IDs

Every request gets a unique correlation ID that flows through:
1. API Gateway → Service
2. Service → Service (HTTP headers)
3. Service → Message Broker
4. All log entries

**Header:** `X-Correlation-Id`

### Structured Logging

**Framework:** Serilog 8.0.0

**Log Levels:**
- **Information:** Service startup, health checks
- **Warning:** Cache misses, degraded dependencies
- **Error:** Failed operations, exceptions
- **Debug:** Detailed execution traces (Development only)

**Log Output:**
- Console (real-time)
- Rolling file (daily rotation, local `/logs` folder)
- Optional: ELK Stack, Application Insights

---

## Deployment Architecture

### Docker Containerization

```dockerfile
# Multi-stage build pattern
FROM mcr.microsoft.com/dotnet/sdk:8.0 as builder
  WORKDIR /src
  COPY . .
  RUN dotnet build -c Release
  RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
  WORKDIR /app
  COPY --from=builder /app .
  EXPOSE 5000-5005
  ENTRYPOINT ["dotnet", "ServiceName.dll"]
```

### Docker Compose Orchestration

```yaml
services:
  - mssql-db:5432 (SQL Server)
  - rabbitmq-service:5672 (RabbitMQ)
  - redis-cache:6379 (Redis)
  - auth-service:5001
  - catalog-service:5002
  - order-service:5003
  - lottery-service:5004
  - notification-service:5005
  - api-gateway:5000

networks:
  - mechira-network (bridge)

volumes:
  - mssql-data
  - mssql-log
  - mssql-backup
  - rabbitmq-data
  - redis-data
```

---

## Security

### Authentication & Authorization

**JWT Bearer Token**
- **Algorithm:** HMAC SHA256
- **Key Length:** 256-bit (32 characters minimum)
- **Issuer:** `AuthService`
- **Expiry:** 60 minutes
- **Refresh:** Supported

**Token Flow:**
```
1. POST /api/auth/login {username, password}
2. AuthService validates credentials
3. Returns JWT token
4. Client sends: Authorization: Bearer {token}
5. Services validate JWT signature
6. ClaimsPrincipal extracted for authorization
```

### Service-to-Service Security

- **JWT validation** on inter-service calls
- **HTTPS** in production (HTTP in development)
- **CORS:** Configured per service
- **Rate Limiting:** Via API Gateway

---

## Performance Characteristics

| Metric | Target | Strategy |
|--------|--------|----------|
| Order Creation | < 500ms | Async saga + event publishing |
| Product Retrieval | < 100ms | Redis cache-aside |
| Auth Validation | < 50ms | JWT in-memory verification |
| Saga Completion | < 5s | Event-driven, non-blocking |

---

## Failure Scenarios & Recovery

### Scenario 1: Database Down
- **Detection:** Health check fails
- **Impact:** All services report 503
- **Recovery:** Database restart, automatic reconnection
- **Data:** Persistent across restarts (volumes)

### Scenario 2: RabbitMQ Down
- **Detection:** Service startup fails (rabbitmq-service dependency)
- **Impact:** Services cannot publish events
- **Recovery:** RabbitMQ restart, manual event replay if needed

### Scenario 3: Redis Down
- **Detection:** CatalogService health = "Degraded"
- **Impact:** Cache miss, slower product queries
- **Recovery:** Redis restart, cache rebuilds automatically

### Scenario 4: Service Crash
- **Detection:** Container exits
- **Recovery:** Docker restart policy = "unless-stopped"
- **Impact:** 10-30 second downtime, in-flight requests fail

---

## Next Steps

1. **Deploy to Kubernetes** - Use YAML manifests for production
2. **Add API Rate Limiting** - Prevent DDoS attacks
3. **Implement Distributed Tracing** - Use Jaeger/Zipkin
4. **Add Metrics Collection** - Prometheus + Grafana
5. **CI/CD Pipeline** - GitHub Actions / Azure DevOps
6. **Security Scanning** - OWASP ZAP, Snyk

---

**Document Version:** 1.0  
**Last Updated:** 2026-07-02  
**Maintainer:** Development Team
