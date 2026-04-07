# Product Management API — Technical Design Report

> Assessment Submission | E-Commerce / Fashion Shop Platform | ASP.NET Core 8

---

## 1. Development Approach

The implementation follows a design-first, layer-by-layer process:

| Step | Activity | Output |
|---|---|---|
| 1 | Domain analysis | Entity list, relationships, edge case inventory |
| 2 | Database schema design | ER diagram, JSONB strategy, index plan |
| 3 | API contract definition | Endpoint list, request/response shapes, HTTP status codes |
| 4 | Project scaffold | Clean Architecture folders, DI wiring, NuGet packages |
| 5 | Layer-by-layer build | Domain → Application → Infrastructure → API |
| 6 | Edge case handling | Concurrency, soft delete, pagination, global error handling |
| 7 | Documentation & testing | Postman collection, README, unit tests |

**Architecture: Clean Architecture** with four layers — Domain has zero external dependencies; Application depends on Domain only; Infrastructure implements Application interfaces; API wires everything together. Each layer is independently testable.

---

## 2. Database Design

### Choice: PostgreSQL

The two key requirements — *scalability* and *strong consistency* — point clearly to a relational database. PostgreSQL provides full ACID compliance by default, native JSONB column support, and handles complex relational queries efficiently.

**Why not MongoDB?** Strong consistency across related collections requires multi-document transactions (v4.0+), adding operational complexity. PostgreSQL gives us consistency for free, plus the ability to query JSONB — no trade-off.

### Schema

```
Category
  id          UUID        PK
  name        VARCHAR(100) NOT NULL
  parent_id   UUID        FK → Category (self-referencing, Restrict delete)
  created_at  TIMESTAMP   UTC
  updated_at  TIMESTAMP   UTC

Product
  id           UUID          PK
  name         VARCHAR(200)  NOT NULL
  description  TEXT
  base_price   DECIMAL(18,2)
  category_id  UUID          FK → Category (Restrict delete)
  status       INT           1=Active | 2=Inactive | 3=Archived
  created_at   TIMESTAMP     UTC
  updated_at   TIMESTAMP     UTC

ProductVariant
  id           UUID          PK
  product_id   UUID          FK → Product (Cascade delete)
  sku          VARCHAR(100)  UNIQUE NOT NULL
  price        DECIMAL(18,2) nullable  ← null means inherit Product.BasePrice
  stock        INT           DEFAULT 0
  attributes   JSONB         e.g. {"size":"M","color":"Black","material":"Cotton"}
  row_version  BYTEA         Optimistic concurrency token
  created_at   TIMESTAMP     UTC
  updated_at   TIMESTAMP     UTC
```

### Dynamic Attributes — JSONB Strategy

Fashion products differ by category: a T-shirt has Size + Color, shoes have Size + Width. Instead of a rigid column-per-attribute schema (breaks with new features) or a full EAV table (complex queries), each `ProductVariant` carries a **JSONB `attributes` column**.

Adding a new attribute (e.g. `"sleeve_length"`) requires **zero schema migrations** — store it as a new JSON key. PostgreSQL can index and query JSONB natively via GIN indexes.

### Indexes

| Table | Index | Purpose |
|---|---|---|
| categories | parent_id | Efficient subtree queries |
| products | category_id | Filter by category |
| products | status | Filter by status |
| product_variants | product_id | Load variants for a product |
| product_variants | sku (UNIQUE) | SKU uniqueness + fast lookup |
| product_variants | attributes (GIN) | JSONB attribute queries |

---

## 3. Technology Stack

| Concern | Technology | Reason |
|---|---|---|
| Framework | ASP.NET Core 8 Web API | LTS, high performance, rich ecosystem |
| Architecture | Clean Architecture | Separation of concerns, testable in isolation |
| ORM | Entity Framework Core 8 + Npgsql | Code-first migrations, built-in optimistic concurrency |
| Database | PostgreSQL 16 | Full ACID, JSONB, production-grade |
| Validation | FluentValidation v11 | Decoupled from models, composable rules, easy to unit test |
| Mapping | Mapster | Faster than AutoMapper, supports DB projection to DTOs |
| Caching | Redis 7 + StackExchange.Redis | Distributed, survives app restarts, multi-instance safe |
| API Docs | Scalar (OpenAPI) | Ships with .NET 8, modern UI |
| Logging | Serilog + rolling file | Structured logs with CorrelationId enrichment |
| Versioning | Asp.Versioning | URL segment + header + query string |
| Rate Limiting | ASP.NET Core built-in RateLimiter | Zero extra dependency, fixed window policies |
| Testing | xUnit + Moq + FluentAssertions | Standard .NET test stack |
| Containers | Docker + Docker Compose | Self-bootstrapping with auto-migration |

---

## 4. API Design

### Endpoints

```
# Categories
GET    /api/v1/categories                   Full category tree (recursive, 5 levels)
GET    /api/v1/categories/{id}              Category by id
POST   /api/v1/categories                   Create category
PUT    /api/v1/categories/{id}              Update category
DELETE /api/v1/categories/{id}              Delete (blocked if has children or products)

# Products
GET    /api/v1/products                     Paged list with filters, search, sort
GET    /api/v1/products/{id}                Product detail with variants
POST   /api/v1/products                     Create product + initial variants
PUT    /api/v1/products/{id}                Update product
DELETE /api/v1/products/{id}                Soft delete (status = Inactive)
GET    /api/v1/products/{id}/variants       All variants for a product
POST   /api/v1/products/{id}/variants       Add variant to product

# Variants
GET    /api/v1/variants/{id}                Variant by id
PUT    /api/v1/variants/{id}                Update variant
PATCH  /api/v1/variants/{id}/stock          Atomic stock adjustment ← critical path
DELETE /api/v1/variants/{id}                Delete (blocked if last variant)
```

### Input Validation Pipeline

Every write request passes through this pipeline before reaching the service layer:

1. Deserialized into a strongly-typed **DTO** — domain entities never exposed directly.
2. **FluentValidation** runs — rejects with `400 Bad Request` + structured `errors` dictionary.
3. Application service applies **business rules** — throws typed exceptions.
4. EF Core generates **parameterized SQL** — SQL injection structurally prevented.

### Response Envelope (success)

```json
{
  "success": true,
  "message": "Request completed successfully.",
  "traceId": "00-abc123...",
  "data": { }
}
```

Error responses always use **RFC 7807 ProblemDetails** — never the envelope format.

### HTTP Status Codes

| Code | Trigger |
|---|---|
| 200 OK | GET, PUT, PATCH success |
| 201 Created | POST success — includes `Location` header |
| 204 No Content | DELETE success |
| 400 Bad Request | FluentValidation failure — includes `errors` dictionary |
| 404 Not Found | Resource does not exist |
| 409 Conflict | Duplicate SKU or concurrency collision |
| 422 Unprocessable Entity | Business rule violation (e.g. insufficient stock) |
| 429 Too Many Requests | Rate limit exceeded — includes `Retry-After` header |
| 500 Internal Server Error | Unexpected error |

---

## 5. Performance

### Caching — Redis, Cache-Aside Pattern

Read-heavy endpoints (`GET /products`, `GET /products/{id}`) are cached in Redis. Cache keys encode all query parameters so different filter combinations get independent entries.

| Endpoint | TTL | Cache key |
|---|---|---|
| GET /products (list) | 60s | `products:list:{page}:{pageSize}:{filters...}` |
| GET /products/{id} | 300s | `products:detail:{id}` |
| GET /categories | 600s | `categories:tree` |

On any successful write (create, update, delete, stock change), relevant cache keys are **explicitly invalidated** — not just left to expire. Clients never see stale data after a mutation.

Redis failure **never crashes the application** — caching is best-effort. If Redis is unavailable, all requests fall through to the database.

### Concurrency — Stock Updates

`PATCH /variants/{id}/stock` is the most sensitive endpoint. Two concurrent requests must not both succeed if only one unit remains. This is handled at two levels:

**Level 1 — Application guard (pre-DB):**
```
if (entity.Stock + request.Quantity < 0)
    throw BusinessException("Insufficient stock...")
```

**Level 2 — Atomic SQL update:**
```sql
UPDATE product_variants
SET    stock      = stock + @quantity,
       updated_at = NOW() AT TIME ZONE 'UTC'
WHERE  id         = @id
  AND  stock + @quantity >= 0
  AND  row_version = @rowVersion   ← optimistic concurrency
```

If `rowsAffected == 0`, the service re-fetches the row to distinguish:
- Stock now insufficient → `422 Unprocessable Entity`
- Row changed by another request → `409 Conflict` (client should retry)

### Rate Limiting

| Policy | Endpoint(s) | Limit |
|---|---|---|
| `stock-adjust` | `PATCH .../stock` | 30 req/min |
| `write-operations` | POST, PUT, DELETE | 60 req/min |
| `read-operations` | GET | 200 req/min |

---

## 6. Environment Variables

| Variable | Example | Description |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=localhost;Port=5432;Database=product_db;Username=postgres;Password=postgres` | PostgreSQL connection string |
| `Redis__ConnectionString` | `localhost:6379` | Redis connection string |
| `ASPNETCORE_ENVIRONMENT` | `Development` | Environment name |
| `ASPNETCORE_URLS` | `https://localhost:7000;http://localhost:5000` | API listen URLs |
| `Cache__ProductListTtlSeconds` | `60` | Product list cache TTL |
| `Cache__ProductDetailTtlSeconds` | `300` | Product detail cache TTL |
| `Cache__CategoryTreeTtlSeconds` | `600` | Category tree cache TTL |

### Running with Docker Compose

```bash
# Start all services (API + PostgreSQL + Redis)
docker compose up -d

# View API logs
docker compose logs -f api

# Health check
curl http://localhost:8080/health
```

Database migrations are applied **automatically on startup** — no manual migration step required.

---

## 7. Postman Collection

The Postman collection is located at:

```
/docs/ProductManagement.postman_collection.json
```

Import it into Postman and configure the following environment variables:

| Variable | Value |
|---|---|
| `base_url` | `https://localhost:7000` |
| `version` | `1` |
| `category_id` | *(auto-populated by test scripts)* |
| `product_id` | *(auto-populated by test scripts)* |
| `variant_id` | *(auto-populated by test scripts)* |

The collection is organized into four folders:

- **Categories** — CRUD + validation edge cases
- **Products** — CRUD + filter/search + edge cases (duplicate SKU, invalid price, missing category)
- **Variants** — CRUD + invalid attribute format
- **Stock Adjustment** — add, deduct, zero quantity, insufficient stock, rate limit (run 35 iterations)

Every request includes `X-Correlation-Id: {{$guid}}` for distributed tracing.

---

## 8. Limitations & Future Improvements

### Current Limitations

| Limitation | Detail |
|---|---|
| No authentication | All endpoints are open. Production requires JWT + RBAC. |
| Single warehouse inventory | Stock is one integer per variant. No multi-warehouse support. |
| No image management | Product images are not handled. Needs blob storage integration. |
| Basic search | Search uses `ILIKE` (PostgreSQL). No typo tolerance or faceted filtering. |
| Synchronous cache invalidation | Invalidation happens in-request. Under high write load, an outbox pattern via message queue would be more resilient. |
| No audit log | No history of who changed what and when. |

### Proposed Future Improvements

- **Authentication & authorization** — JWT with role-based access control (Admin vs. Storefront).
- **Elasticsearch / Typesense** — Replace `ILIKE` search with a dedicated search engine for relevance, typo tolerance, and faceted filtering.
- **Product media service** — Image upload/storage backed by Azure Blob or S3.
- **Multi-warehouse inventory** — Separate `InventoryLocation` table with stock per warehouse.
- **Outbox pattern** — Reliable cache invalidation via domain events + message queue (RabbitMQ / Azure Service Bus).
- **Rate limiting per user** — Current rate limiting is per IP. Authenticated users should have per-account limits.
- **Integration tests** — `Testcontainers` for spinning up real PostgreSQL + Redis in CI.
- **GitHub Actions CI** — Automated build, test, and Docker image push on every PR.
- **Health check UI** — `/health` endpoint already in place; add `AspNetCore.HealthChecks.UI` for a visual dashboard.