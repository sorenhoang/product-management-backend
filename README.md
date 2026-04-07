# Product Management API

A RESTful API for managing products in a retail/e-commerce application (fashion shop), built with **ASP.NET Core 8** and **PostgreSQL**.

---

## Table of Contents

- [Overview](#overview)
- [Development Approach](#development-approach)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Database Design](#database-design)
- [API Design](#api-design)
- [Performance: Caching & Concurrency](#performance-caching--concurrency)
- [Environment Variables](#environment-variables)
- [Getting Started](#getting-started)
- [Postman Collection](#postman-collection)
- [Edge Cases Covered](#edge-cases-covered)
- [Limitations & Future Improvements](#limitations--future-improvements)

---

## Overview

This project implements product management endpoints for a fashion e-commerce platform, with a focus on:

- **Scalability** — Clean Architecture, Redis caching, and efficient indexed queries
- **Strong consistency** — PostgreSQL ACID transactions, atomic stock updates, and EF Core optimistic concurrency
- **Extensibility** — JSONB-based dynamic product attributes requiring no schema migration for new features

---

## Development Approach

The implementation followed these steps in order:

1. **Domain analysis** — Identified core entities (Product, ProductVariant, Category) and their relationships. Mapped out the fashion-specific challenge: a single product has multiple variants (size × color), each with its own SKU, price, and stock.
2. **Database schema design** — Finalized the ER diagram before writing any code to avoid costly structural changes mid-project.
3. **API contract definition** — Defined all endpoints, request/response shapes, and HTTP status codes upfront.
4. **Project scaffolding** — Set up Clean Architecture layers, EF Core with migrations, and dependency injection wiring.
5. **Layer-by-layer implementation** — Domain → Application (use cases + validators) → Infrastructure (repositories, caching) → API (controllers, middleware).
6. **Edge case handling** — Concurrency on stock updates, soft delete, pagination/filtering, and global error handling.
7. **Documentation and testing** — Postman collection, README, and environment configuration.

---

## Tech Stack

| Concern | Technology | Reason |
|---|---|---|
| Framework | ASP.NET Core 8 Web API | LTS release, high performance, rich ecosystem |
| ORM | Entity Framework Core 8 | Code-first migrations, LINQ, built-in optimistic concurrency |
| Database | PostgreSQL + Npgsql | Full ACID, JSONB column support, production-grade |
| Validation | FluentValidation | Validation logic decoupled from models; easy to unit test |
| Mapping | Mapster | Faster than AutoMapper; supports direct DB projection to DTOs |
| Caching | Redis + StackExchange.Redis | Distributed, survives app restarts, supports multi-instance deployments |
| API Docs | Scalar (OpenAPI) | Ships with .NET 8; modern UI |
| Logging | Serilog | Structured logs; queryable in production |
| Architecture | Clean Architecture | Clear separation of concerns; Domain and Application layers have zero infrastructure dependencies |

---

## Architecture

The project is organized into four layers following Clean Architecture principles:

```
src/
├── Domain/           # Entities, value objects, domain rules
├── Application/      # Use cases, DTOs, interfaces, FluentValidation validators
├── Infrastructure/   # EF Core DbContext, Redis cache, repository implementations
└── API/              # Controllers, middleware, Program.cs, Scalar/Swagger
```

Dependencies flow inward only: `API → Application → Domain`. Infrastructure implements interfaces defined in Application — the domain has no knowledge of persistence or HTTP.

---

## Database Design

**Database: PostgreSQL**

PostgreSQL was chosen over a NoSQL alternative for two reasons that directly match the project requirements:

- **Strong consistency** is provided natively via full ACID compliance. No extra configuration required.
- **Scalability** for product management workloads (structured relationships, complex filters, transactional stock updates) is well-served by a relational model.

### Why not MongoDB?

MongoDB's flexible schema is appealing for dynamic attributes, but strong consistency across related collections requires multi-document transactions (available since v4.0), adding operational complexity. PostgreSQL gives us consistency by default, plus the ability to query JSONB — so there is no trade-off.

### Schema

```
Category
  id            UUID        PK
  name          VARCHAR     NOT NULL
  parent_id     UUID        FK → Category (self-referencing, nullable — top-level categories)
  created_at    TIMESTAMP

Product
  id            UUID        PK
  name          VARCHAR     NOT NULL
  description   TEXT
  base_price    DECIMAL     NOT NULL
  category_id   UUID        FK → Category
  status        ENUM        (Active, Inactive, Archived)
  created_at    TIMESTAMP
  updated_at    TIMESTAMP

ProductVariant
  id            UUID        PK
  product_id    UUID        FK → Product
  sku           VARCHAR     UNIQUE NOT NULL
  price         DECIMAL     NOT NULL (nullable = inherits base_price)
  stock         INT         NOT NULL DEFAULT 0
  attributes    JSONB       e.g. {"size": "M", "color": "Black", "material": "Cotton"}
  row_version   BYTEA       Optimistic concurrency token ([Timestamp])
  created_at    TIMESTAMP
  updated_at    TIMESTAMP
```

### Handling dynamic product attributes

Fashion products differ by category: a T-shirt has Size + Color, shoes have Size + Width, a bag has Material + Dimensions. Rather than a rigid column-per-attribute schema or a full EAV table, each `ProductVariant` carries an `attributes` **JSONB column**.

This means:
- Adding a new attribute (e.g., `"sleeve_length"`) requires **zero schema migrations** — it is stored as a new JSON key.
- PostgreSQL can **index and query JSONB** natively via GIN indexes.
- The Application layer validates known attribute keys via FluentValidation rules per category.

### Indexes

```sql
CREATE INDEX idx_products_category    ON products(category_id);
CREATE INDEX idx_products_status      ON products(status);
CREATE INDEX idx_variants_product     ON product_variants(product_id);
CREATE INDEX idx_variants_sku         ON product_variants(sku);
CREATE INDEX idx_variants_attributes  ON product_variants USING GIN(attributes);
```

---

## API Design

All endpoints are versioned under `/api/v1`.

### Products

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/v1/products` | List products with filter, search, and pagination |
| `POST` | `/api/v1/products` | Create a product with initial variants |
| `GET` | `/api/v1/products/{id}` | Get product detail including variants |
| `PUT` | `/api/v1/products/{id}` | Full product update |
| `DELETE` | `/api/v1/products/{id}` | Soft delete (sets status = `Inactive`) |

### Variants

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/v1/products/{id}/variants` | List variants for a product |
| `POST` | `/api/v1/products/{id}/variants` | Add a new variant |
| `PUT` | `/api/v1/variants/{id}` | Update variant details |
| `PATCH` | `/api/v1/variants/{id}/stock` | Atomic stock adjustment (critical path) |
| `DELETE` | `/api/v1/variants/{id}` | Soft delete variant |

### Categories

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/v1/categories` | Get full category tree |
| `POST` | `/api/v1/categories` | Create a category (supports parent_id for hierarchy) |

### Query parameters for `GET /api/v1/products`

| Parameter | Type | Description |
|---|---|---|
| `page` | int | Page number (default: 1) |
| `pageSize` | int | Items per page (default: 20, max: 100) |
| `search` | string | Full-text search on name and description |
| `categoryId` | UUID | Filter by category |
| `status` | string | Filter by status (Active / Inactive / Archived) |
| `minPrice` | decimal | Minimum base price |
| `maxPrice` | decimal | Maximum base price |
| `sortBy` | string | Field to sort by (createdAt, name, basePrice) |
| `sortOrder` | string | asc / desc |

### Input validation pipeline

1. Request is deserialized into a strongly-typed **DTO** — domain entities are never exposed directly.
2. **FluentValidation** runs before the handler. Invalid requests receive `400 Bad Request` with a structured error body listing all violated rules.
3. The Application layer maps the DTO to a domain command and applies business rules.
4. EF Core generates **parameterized SQL** — SQL injection is structurally prevented at the ORM level.

### Output and error conventions

- All responses return typed **response DTOs**, never raw EF entities (prevents circular references and controls what data is exposed).
- Paginated list responses always include `{ data, totalCount, page, pageSize, totalPages }`.
- A global exception middleware catches unhandled exceptions and returns **RFC 7807 ProblemDetails** — consistent error format across all endpoints.

| Status Code | Meaning |
|---|---|
| `200 OK` | Successful GET / PATCH |
| `201 Created` | Successful POST (includes `Location` header) |
| `204 No Content` | Successful DELETE |
| `400 Bad Request` | Validation failure |
| `404 Not Found` | Resource not found |
| `409 Conflict` | Optimistic concurrency violation or insufficient stock |
| `422 Unprocessable Entity` | Business rule violation (e.g., duplicate SKU) |
| `500 Internal Server Error` | Unexpected server error (ProblemDetails body) |

---

## Performance: Caching & Concurrency

### Caching — Redis, cache-aside pattern

Read-heavy endpoints (`GET /products`, `GET /products/{id}`) are cached in Redis with a TTL of 60–300 seconds depending on update frequency. Cache keys are derived from all query parameters so different queries get independent entries.

On any successful write (create, update, delete, stock change), **relevant cache keys are explicitly invalidated** — not just expired by TTL. This ensures clients never observe stale product data after a successful mutation.

```
Cache key examples:
  products:list:page=1:pageSize=20:categoryId=xxx:status=Active
  products:detail:id=yyy
```

### Concurrency — stock updates

`PATCH /variants/{id}/stock` is the most sensitive endpoint. Two concurrent requests (e.g., two customers buying the last item) must not both succeed if only one unit remains.

**Level 1 — Optimistic concurrency via EF Core `[Timestamp]`:**

The `ProductVariant` entity carries a `RowVersion` byte array. EF Core includes this in every `UPDATE` statement's `WHERE` clause. If two concurrent requests reach the same row, one will find zero rows affected and EF throws `DbUpdateConcurrencyException`. The API catches this and returns `409 Conflict`.

**Level 2 — Atomic stock decrement at the database level:**

For stock adjustments specifically, a read-then-write pattern is avoided entirely. Instead, stock is decremented atomically:

```sql
UPDATE product_variants
SET stock = stock - @quantity, updated_at = NOW()
WHERE id = @id AND stock >= @quantity
```

If `rowsAffected == 0`, the stock was insufficient — the API returns `409 Conflict` with a clear message. This eliminates the possibility of stock going negative regardless of how many concurrent requests arrive simultaneously.

---

## Environment Variables

Create a `.env` file or set the following in `appsettings.json` / host environment:

```env
# Database
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=product_db;Username=postgres;Password=yourpassword

# Redis
Redis__ConnectionString=localhost:6379

# API
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=https://localhost:7000;http://localhost:5000

# Cache TTL (seconds)
Cache__ProductListTtl=60
Cache__ProductDetailTtl=300
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 15+](https://www.postgresql.org/)
- [Redis](https://redis.io/)

### Run locally

```bash
# 1. Clone the repository
git clone https://github.com/your-username/product-management-api.git
cd product-management-api

# 2. Configure environment variables
cp appsettings.Example.json appsettings.Development.json
# Edit the connection strings inside appsettings.Development.json

# 3. Apply database migrations
dotnet ef database update --project src/Infrastructure --startup-project src/API

# 4. Run the API
dotnet run --project src/API

# 5. Open API documentation
# https://localhost:7000/scalar/v1
```

### Run with Docker Compose

```bash
docker-compose up --build
```

The `docker-compose.yml` spins up the API, PostgreSQL, and Redis together.

---

## Postman Collection

A full Postman collection is included at `/docs/ProductManagement.postman_collection.json`.

Import it into Postman and set the following environment variables:

| Variable | Value |
|---|---|
| `base_url` | `https://localhost:7000` |
| `product_id` | _(populated automatically by collection scripts)_ |
| `variant_id` | _(populated automatically by collection scripts)_ |

The collection is organized into folders: **Categories**, **Products**, **Variants**, and **Edge Cases** (concurrent stock update, duplicate SKU, invalid pagination, etc.).

---

## Edge Cases Covered

| Scenario | Handling |
|---|---|
| Concurrent stock decrement | Atomic SQL `WHERE stock >= qty`; returns `409` if insufficient |
| Optimistic concurrency conflict | EF Core `RowVersion`; returns `409 Conflict` |
| Duplicate SKU on variant creation | Unique DB constraint + `422 Unprocessable Entity` |
| Negative stock | Prevented at DB level by the atomic update condition |
| Product with no variants | Allowed; variants are added separately |
| Category hierarchy (deep nesting) | Self-referencing FK; tree traversal via recursive CTE |
| Soft delete cascade | Deleting a product marks all its variants as `Inactive` |
| Invalid pagination parameters | FluentValidation rejects `page < 1` or `pageSize > 100` |
| Empty search string | Treated as no filter; full list is returned |
| Update non-existent resource | Returns `404 Not Found` with descriptive message |
| JSONB attribute with unknown keys | Accepted but flagged in response if category schema is defined |

---

## Limitations & Future Improvements

### Current limitations

- **No authentication/authorization** — endpoints are open. A production system would require JWT-based auth with role checks (admin vs. storefront).
- **Single warehouse inventory** — stock is a single integer per variant. Multi-warehouse inventory would require a separate `InventoryLocation` table.
- **No image management** — product images are not handled. A production system would integrate with blob storage (e.g., Azure Blob, S3).
- **In-process Redis invalidation** — cache invalidation is done synchronously in the same request. Under high write load, a message-queue-based invalidation pattern (e.g., via RabbitMQ or Azure Service Bus) would be more resilient.
- **No full-text search engine** — search is implemented as a PostgreSQL `ILIKE` query. For advanced search (typo tolerance, faceted filtering), Elasticsearch or Typesense would be a better fit.

### Proposed future improvements

- Add JWT authentication with role-based access control (RBAC).
- Integrate an outbox pattern for reliable cache invalidation via domain events.
- Add multi-warehouse inventory support.
- Replace `ILIKE` search with Elasticsearch for better search performance and relevance.
- Introduce a product media service backed by cloud blob storage.
- Add rate limiting (ASP.NET Core built-in rate limiter, .NET 8+).
- Implement health check endpoints (`/health`, `/health/ready`) for Kubernetes probes.
- Add integration tests using `Testcontainers` for PostgreSQL and Redis.