# ADR-002: SQL Server For All Services (No Polyglot in Phase 1)

## Status
✅ **ACCEPTED**

## Date
2026-07-02

## Context
During Phase 1 database isolation, we considered whether to use **polyglot persistence** (different database technologies per service). Specifically:
- **Option A:** Keep all services on SQL Server (current choice)
- **Option B:** Use MongoDB for CatalogService (document database for product catalog)
- **Option C:** Mixed approach (SQL for Orders, NoSQL for others)

## Decision
**Keep all services on SQL Server in Phase 1.**

- AuthService: SQL Server (`Mechira-AuthService`)
- CatalogService: SQL Server (`Mechira-CatalogService`)
- OrderService: SQL Server (`Mechira-OrderService`)
- LotteryService: SQL Server (`Mechira-LotteryService`)
- NotificationService: SQL Server (`Mechira-NotificationService`)

**Polyglot persistence can be added in bonus phases** (Phase 4+) after core microservices are working.

## Rationale

### 1. **Simplicity & Time-to-Value** ✅
- Single database technology = single ops workflow
- Team already knows SQL Server
- No new driver/connection libraries
- Focus Phase 1 on data **isolation**, not technology diversity

### 2. **Each Service Has Different Consistency Needs, Not Different Tech Needs** ✅
| Service | Consistency Need | Why SQL is Sufficient |
|---------|-----------------|----------------------|
| **Orders** | ACID required (money) | SQL transactions ✅ |
| **Auth** | Strong consistency | SQL queries ✅ |
| **Catalog** | Eventual consistency | SQL + Redis cache ✅ (Phase 4) |
| **Lottery** | Time-series data | SQL ✅ |
| **Notifications** | Log of events | SQL audit logs ✅ |

- None of these *require* NoSQL; they just have different consistency models
- Redis (Phase 4, already implemented) handles caching needs

### 3. **Operational Overhead of Polyglot** ⚠️
If we added MongoDB:
- Separate connection pooling
- Different backup tools
- Different monitoring/alerting
- Different troubleshooting skills required
- Data migration tools (if we want to switch later)

### 4. **SQL Server Already Handles Most Use Cases** ✅
Modern SQL Server supports:
- **Document model:** SQL Server JSON storage (`FOR JSON PATH`)
- **Time-series:** Built-in time-series tables (`GENERATE_SERIES`)
- **Caching:** Redis used for read performance (Phase 4)
- **Eventual consistency:** Application-level implementation (saga pattern)

### 5. **Future Flexibility** 🔄
If polyglot becomes necessary later:
- Each service's data is already isolated (no cross-service queries to refactor)
- Can migrate CatalogService to MongoDB independently
- Can add Elasticsearch for product search (Phase 4+)
- Can add Neo4j for "customers also bought" (Phase 4+)

## CAP Theorem Alignment

Each service's database choice reflects its consistency requirements:

| Service | Consistency | Availability | Partition Tolerance | Choice |
|---------|-------------|--------------|---------------------|--------|
| **Orders** | Must-have | Important | Tolerate | **CP** (SQL ACID) |
| **Catalog** | Eventually OK | Must-have | Tolerate | **CP** (SQL + cache) |
| **Auth** | Must-have | Important | Tolerate | **CP** (SQL transactions) |

- All services choose **Consistency over Partition Tolerance** (CP model)
- High availability achieved via load balancing (Phase 3) and caching (Phase 4)

## Consequences

### Positive ✅
- **Faster Phase 1 delivery:** Focus on isolation, not technology integration
- **Operational simplicity:** Single database team/tooling
- **No blocker:** Doesn't prevent Phase 2-5 implementation
- **Future-proof:** Easier to add polyglot in bonus phases

### Negative ⚠️
- **Missed opportunity:** Catalog *could* use document model benefits
  - **Mitigation:** Can be added in Phase 4 bonus as stretch goal
- **No vendor diversity:** Single vendor lock-in
  - **Mitigation:** SQL Server is industry standard; migration is trivial

## Alternatives Considered

### 1. ❌ Full Polyglot (MongoDB + Redis + SQL + Elasticsearch)
- **Pros:** Each service uses "best fit" tech
- **Cons:** Massive operational overhead, slows Phase 1, requires multiple ops skills
- **Status:** Rejected for Phase 1, kept as Phase 4 bonus

### 2. ❌ MongoDB for Catalog Only
- **Pros:** Document model fits varying product schemas
- **Cons:** Adds complexity to Phase 1, requires different ops knowledge
- **Status:** Rejected for Phase 1; can do in bonus phase

### 3. ✅ **SQL Server For All (CHOSEN)**
- **Pros:** Simple, team knows it, can extend with polyglot later
- **Cons:** Misses opportunity for tech diversity
- **Status:** Accepted for Phase 1 (not final decision)

## Implementation

### Current Setup
All services use identical connection pattern:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Mechira-{ServiceName};..."
  }
}
```

### Future Polyglot Migration Path

**Phase 4+ (Bonus): To add MongoDB for CatalogService:**

1. Create new MongoDB container in docker-compose
2. Move CatalogService to MongoDB (new DbContext, migrations)
3. Update CatalogService connection string to MongoDB driver
4. Implement MongoDB-specific repository layer
5. Cache product reads via Redis (already implemented)

Example:
```yaml
# Add to docker-compose.yml (Phase 4)
mongodb:
  image: mongo:6.0
  ports: ["27017:27017"]
  volumes: [mongo-data:/data/db]

catalog-service:
  environment:
    ConnectionStrings__DefaultConnection: "mongodb://mongodb:27017/Mechira-Catalog"
```

## Related Decisions
- **ADR-001:** Database-per-service pattern (technology-agnostic)
- **Phase 4:** Redis caching handles read performance for Catalog
- **Phase 4 Bonus:** Polyglot can be added (MongoDB, Elasticsearch, Neo4j)

## Review & Reconsideration

**When to reconsider this decision:**
- If CatalogService reads become bottleneck (add MongoDB document caching)
- If product schema variations explode (migrate to MongoDB document model)
- If search requirements exceed SQL full-text (add Elasticsearch)
- If recommendation engine needed (add Neo4j graph database)

**Not a permanent decision:** This is Phase 1. Polyglot is explicitly allowed in bonus phases.

## References
- MongoDB vs SQL Server: Document vs relational tradeoffs
- CAP Theorem: Choosing consistency models per service
- SQL Server JSON: `FOR JSON PATH` for document-like queries
- Redis caching: Cache-aside pattern (Phase 4)
