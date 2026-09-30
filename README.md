# HighPerformanceCatalogApi

A .NET 10 read-heavy catalog API built to demonstrate evidence-based performance engineering with PostgreSQL, Redis, ASP.NET Core Output Caching, OpenTelemetry, Prometheus, Testcontainers, and k6.

The domain is deliberately small so the repository can focus on measurable query and caching behavior rather than business complexity.

## Quick start

Prerequisite: Docker with Docker Compose.

```bash
docker compose up --build
```

The first startup applies the EF Core migration and deterministically seeds 100,000 products. The seed size is configurable through `SEED_PRODUCT_COUNT` in a local `.env` copied from `.env.example`.

When the API is healthy:

```text
API:        http://localhost:8080
Health:     http://localhost:8080/health
Metrics:    http://localhost:8080/metrics
Prometheus: http://localhost:9090
```

Stop the stack while preserving PostgreSQL data:

```bash
docker compose down
```

To remove the local database volume and run the deterministic seed again:

```bash
docker compose down --volumes
docker compose up --build
```

`down --volumes` deletes the local Compose database volume.

## Endpoints

```http
GET /api/products/1
GET /api/products?page=1&pageSize=50
GET /api/products?afterId=50&pageSize=50
GET /api/products/search?categoryId=1&minPrice=10&maxPrice=500&minRating=4&page=1&pageSize=50
GET /api/products/top?take=20
GET /health
GET /metrics
```

Page sizes are limited to 200. Offset pages return `hasNextPage`; keyset pages use the stable indexed product ID and return `nextAfterId`. Responses project directly to read DTOs and never expose EF entities.

## Performance design

- Read queries use `AsNoTracking()` and server-side DTO projection.
- Listing and search fetch `pageSize + 1` rows to report whether another page exists without a separate count query.
- `ix_products_category_price_id` supports category/price search, while `ix_products_rating_id` supports top-product ordering.
- `GET /api/products/{id}` uses Redis cache-aside with key `catalog:product:v1:{id}` and a default 60-second TTL. A miss or Redis failure falls back to PostgreSQL; Redis is never the source of truth.
- `GET /api/products/top` uses HTTP Output Cache for 30 seconds and varies by `take`. Error responses are not cached by the default policy.
- Cache freshness is TTL-based because V1 has no write workflow. Distributed invalidation is intentionally out of scope.

The Redis cache and Output Cache demonstrate different layers: Redis reuses application data, while Output Cache reuses a complete HTTP response.

## Local build and tests

The SDK is pinned by the target framework to .NET 10. Restore uses the repository NuGet configuration:

```bash
dotnet tool restore --configfile NuGet.Config
dotnet restore HighPerformanceCatalogApi.slnx --configfile NuGet.Config
dotnet build HighPerformanceCatalogApi.slnx --no-restore
dotnet test tests/Catalog.UnitTests/Catalog.UnitTests.csproj --no-build --no-restore
dotnet test tests/Catalog.IntegrationTests/Catalog.IntegrationTests.csproj --no-build --no-restore
```

Integration tests require a running Docker daemon. Testcontainers starts isolated PostgreSQL and Redis containers and uses a small 120-product deterministic seed.

Create a future migration with:

```bash
dotnet tool run dotnet-ef migrations add MigrationName \
  --project src/Catalog.Infrastructure/Catalog.Infrastructure.csproj \
  --startup-project src/Catalog.Api/Catalog.Api.csproj \
  --output-dir Migrations
```

## Load tests

Install k6 or run it through its official container image. With the API running:

```bash
k6 run load-tests/baseline.js
k6 run load-tests/cached.js
k6 run load-tests/output-cache.js
k6 run load-tests/pagination.js
```

Example repeatable run:

```bash
BASE_URL=http://localhost:8080 VUS=30 DURATION=60s k6 run load-tests/baseline.js
```

The scenarios report k6's request rate, average latency, p50, p95, p99, checks, and error rate. Record only actual runs in `docs/PERFORMANCE-RESULTS.md`.

## Observability

OpenTelemetry instruments ASP.NET Core, outgoing HTTP, EF Core, and the .NET runtime. Custom low-cardinality signals are:

```text
catalog_cache_hits_total
catalog_cache_misses_total
catalog_search_requests_total
```

Prometheus scrapes the API every five seconds. Set `OpenTelemetry__OtlpEndpoint` to an OTLP endpoint when trace/metric export to a collector is needed; it is disabled by default.

## Configuration

Safe defaults live in `src/Catalog.Api/appsettings.json`. Runtime settings use standard ASP.NET Core environment variable mapping, for example:

```text
ConnectionStrings__Postgres
ConnectionStrings__Redis
Seed__Enabled
Seed__ProductCount
Seed__BatchSize
Cache__ProductTtlSeconds
OutputCache__TopProductsSeconds
OpenTelemetry__OtlpEndpoint
```

Never commit a populated `.env` or production credentials.

## Structure

```text
src/Catalog.Domain          Core entities
src/Catalog.Application     Read DTOs, contracts, validation, query service
src/Catalog.Infrastructure  EF Core, PostgreSQL, Redis, migrations, seed, health
src/Catalog.Api             Minimal HTTP entry point and telemetry composition
tests/Catalog.UnitTests
tests/Catalog.IntegrationTests
load-tests
prometheus
```

See `docs/` for scope, architecture, performance methodology, testing, observability, and engineering constraints.
