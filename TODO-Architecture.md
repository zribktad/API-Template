# Architecture: Microservices with A-Frame + Wolverine + RabbitMQ

## Status: IMPLEMENTED

All 7 microservices extracted from the monolith and running independently.

---

## Open TODOs

### [x] Saga Reliability — Missing EF Core Persistence Mapping
Neither `ProductDeletionSaga` (ProductCatalog) nor `TenantDeactivationSaga` (Identity) is mapped in the service's `DbContext`, and no EF Core migration exists for either saga table.
With `UseEntityFrameworkCoreTransactions()`, Wolverine requires the saga type to be present in the registered `DbContext` to persist state between messages.
Required for each saga:
1. Add `DbSet<XxxSaga>` to the service's `DbContext`.
2. Add an `IEntityTypeConfiguration<XxxSaga>` that maps `Id`, status flags, and excludes global query filters (sagas are not tenant-scoped).
3. Generate and run `dotnet ef migrations add AddXxxSaga`.

### [x] Saga Reliability — Missing Timeout Handling
Neither `ProductDeletionSaga` nor `TenantDeactivationSaga` implement timeout/expiration.
If a downstream service fails to publish its completion message the saga will remain in an incomplete state indefinitely with no compensation or alerting.
Implement `ScheduleTimeout<T>(TimeSpan)` in Wolverine for both sagas, with a compensation handler that logs/alerts and optionally rolls back the parent operation.

### Reliability
- Replace in-memory `ChannelJobQueue` in BackgroundJobs with durable persistence/replay so queued jobs survive process restarts.
- Replace in-memory `ChannelEmailQueue` in Notifications with durable persistence/replay so pending emails are not lost on restart between enqueue and delivery.

### File Storage Cascade
- Implement real product-related file cleanup in FileStorage on `ProductDeletedIntegrationEvent`.
- Introduce an explicit relation from stored files to product-owned resources, so `FilesCascadeCompleted` is emitted only after actual cleanup instead of a placeholder acknowledgment (currently publishes `FilesCascadeCompleted` with `count = 0`).

### Messaging — Missing Idempotency Keys in Integration Events
All integration events (`UserRegisteredIntegrationEvent`, `TenantDeactivatedIntegrationEvent`, etc.) lack a `MessageId` field.
On RabbitMQ redelivery (e.g. after a consumer crash mid-processing) the same event can be processed twice, causing duplicate emails, duplicate cascade deletes, etc.
Add `Guid MessageId` to all integration event records and guard handlers with an idempotency check against a seen-message store.

### DRY — Duplicate Validator Code
`ProductValidationRules` and `ProductRequestValidatorBase<T>` are copy-pasted identically into:
- `src/APITemplate.Application/Features/Product/Validation/ProductRequestValidatorBase.cs`
- `src/Services/ProductCatalog/ProductCatalog.Application/Features/Product/Validation/ProductRequestValidatorBase.cs`

Consolidate into `SharedKernel.Application` (or `Contracts`) so both services share the same source of truth.

### Test Coverage
- Add integration/runtime coverage for shared auth bootstrap: permission policies, BFF cookie/OIDC flow, tenant claim enrichment, and locked-down Wolverine HTTP endpoints.
- Add saga-flow tests that verify `ProductDeletionSaga` correlation across ProductCatalog, Reviews, and FileStorage using the new `CorrelationId`.
- Add saga-flow tests for `TenantDeactivationSaga`.

---

### Regressions vs. Monolith (main branch)

#### CRITICAL — Output Caching Not Ported

The monolith had a full tenant-aware output caching stack that was never ported to the microservices architecture:

- `TenantAwareOutputCachePolicy` — varied cache key by tenant ID so responses were never served across tenant boundaries. **Not in SharedKernel, not in any service.**
- `CacheInvalidationHandler` (Wolverine) — handled `CacheInvalidationNotification` and called `IOutputCacheInvalidationService.EvictAsync`. **No equivalent exists in any microservice.**
- `IOutputCacheInvalidationService` / `OutputCacheInvalidationService` — Redis/Dragonfly-backed tag eviction with telemetry. **Not in SharedKernel.**
- `AddOutputCache` + `UseOutputCache` — never registered in any microservice `Program.cs` or SharedKernel.
- `[OutputCache(PolicyName = CacheTags.X)]` decorators — missing from all controllers in ProductCatalog, Identity, FileStorage, BackgroundJobs, Webhooks, Notifications. Reviews has the decorators but the middleware is not registered → decorators are silently ignored at runtime.
- `CacheTags` constants and `CacheInvalidationNotification` records in Reviews are dead code without a handler.

Required fixes:
- Move `TenantAwareOutputCachePolicy`, `IOutputCacheInvalidationService`, `OutputCacheInvalidationService`, `CacheInvalidationHandler`, and `CacheTags` into `SharedKernel.Api`.
- Register `AddOutputCache` (with Redis/Dragonfly backing and `TenantAwareOutputCachePolicy`) in a shared extension method and call it from every microservice `Program.cs`.
- Add `[OutputCache(PolicyName = CacheTags.X)]` to all GET endpoints in ProductCatalog, Identity, FileStorage, and other read-heavy services.
- Publish `CacheInvalidationNotification` in write command handlers and ensure the Wolverine `CacheInvalidationHandler` is discovered in each service.

#### Missing — Demo/Example Endpoints Not in Gateway

Three demonstration controllers exist in the monolith (`src/APITemplate.Api/`) and their SharedKernel infrastructure was already ported (idempotency store, permissions), but the controllers themselves were never added to the Gateway or any microservice:

- `IdempotentController` — demonstrates idempotent POST semantics via `[Idempotent]` filter and `IIdempotencyStore`.
- `PatchController` — demonstrates JSON Patch (RFC 6902) partial updates.
- `SseController` — demonstrates Server-Sent Events streaming over a persistent HTTP connection.

Required fix: decide whether these should live in a dedicated `Examples` microservice or directly in the Gateway API, then add the controllers and register the route in YARP.

---

## Bounded Contexts (Implemented)

| # | Service | Entities | Database | Transport |
|---|---------|----------|----------|-----------|
| 1 | **Product Catalog** | Product, Category, ProductData, ProductDataLink, ProductCategoryStats | PostgreSQL + MongoDB | RabbitMQ (product-catalog.events) |
| 2 | **Reviews** | ProductReview, ProductProjection | PostgreSQL | RabbitMQ (reviews.events) |
| 3 | **Identity & Tenancy** | AppUser, Tenant, TenantInvitation | PostgreSQL + Keycloak | RabbitMQ (identity.events) |
| 4 | **Notifications** | FailedEmail | PostgreSQL | RabbitMQ (consumer only) |
| 5 | **File Storage** | StoredFile | PostgreSQL + filesystem | RabbitMQ (consumer only) |
| 6 | **Background Jobs** | JobExecution | PostgreSQL + TickerQ | RabbitMQ (consumer only) |
| 7 | **Webhooks** | WebhookSubscription, DeliveryLog, EventType | PostgreSQL | RabbitMQ (consumer only) |

---

## Architecture Patterns

### A-Frame Architecture (per handler)
- **Load** (Infrastructure) - fetch data, validate existence
- **Handle** (Domain Logic) - pure business decisions
- **Conductor** (Wolverine) - orchestrates Load -> Handle -> side effects

### Clean Architecture (per service)
- Domain -> Application -> Infrastructure -> Api
- SharedKernel for cross-cutting concerns (5 projects)

### Vertical Slice (per feature)
- Co-located command/query + handler + validator + DTOs in Application layer

### DDD
- Aggregate roots per bounded context
- Integration events for cross-service communication
- Saga orchestration for distributed workflows

---

## Solution Structure

```
src/
  SharedKernel/
    SharedKernel.Domain/              # Entity contracts, value objects, exceptions
    SharedKernel.Application/         # ErrorOr middleware, batch, DTOs, queue abstractions
    SharedKernel.Infrastructure/      # TenantAuditableDbContext, UoW, RepositoryBase, queue impl
    SharedKernel.Messaging/           # Wolverine conventions, RabbitMQ topology, retry policies
    SharedKernel.Api/                 # Base controller, ErrorOr mapping, auth, observability, DI helpers
  Contracts/
    Contracts.IntegrationEvents/      # Integration events + saga messages
  Services/
    {ServiceName}/
      {ServiceName}.Domain/
      {ServiceName}.Application/
      {ServiceName}.Infrastructure/
      {ServiceName}.Api/
  Gateway/
    Gateway.Api/                      # YARP reverse proxy
tests/
  {ServiceName}.Tests/                # Unit tests per service
```

---

## Messaging Topology (RabbitMQ)

### Exchanges (fanout, durable)
| Exchange | Publisher |
|----------|-----------|
| `identity.events` | Identity |
| `product-catalog.events` | Product Catalog |
| `reviews.events` | Reviews |

### Queue Bindings
| Queue | Exchange | Consumer |
|-------|----------|----------|
| `reviews.product-created` | product-catalog.events | Reviews |
| `reviews.product-deleted` | product-catalog.events | Reviews |
| `reviews.tenant-deactivated` | identity.events | Reviews |
| `file-storage.product-deleted` | product-catalog.events | File Storage |
| `notifications.user-registered` | identity.events | Notifications |
| `notifications.user-role-changed` | identity.events | Notifications |
| `notifications.invitation-created` | identity.events | Notifications |
| `webhooks.product-created` | product-catalog.events | Webhooks |
| `webhooks.product-deleted` | product-catalog.events | Webhooks |
| `webhooks.review-created` | reviews.events | Webhooks |
| `webhooks.category-deleted` | product-catalog.events | Webhooks |
| `background-jobs.tenant-deactivated` | identity.events | Background Jobs |
| `identity.tenant-deactivated` | identity.events | Identity |
| `identity.users-cascade-completed` | — (direct) | Identity |
| `identity.products-cascade-completed` | — (direct) | Identity |
| `identity.categories-cascade-completed` | — (direct) | Identity |
| `product-catalog.tenant-deactivated` | identity.events | ProductCatalog |

---

## Sagas

### ProductDeletionSaga (Product Catalog)
- Triggered by `StartProductDeletionSaga` from DeleteProductsCommand
- Publishes `ProductDeletedIntegrationEvent` to cascade to Reviews + FileStorage
- Waits for `ReviewsCascadeCompleted` + `FilesCascadeCompleted`

### TenantDeactivationSaga (Identity)
- Triggered by `StartTenantDeactivationSaga` from DeleteTenantCommand
- Publishes `TenantDeactivatedIntegrationEvent` to all dependent services
- Waits for `UsersCascadeCompleted` + `ProductsCascadeCompleted` + `CategoriesCascadeCompleted`

---

## Infrastructure (Docker Compose)

| Service | Image | Port | Purpose |
|---------|-------|------|---------|
| RabbitMQ | rabbitmq:4-management | 5672, 15672 | Message broker |
| PostgreSQL | postgres:17 | 5432 | 7 databases (one per service) |
| MongoDB | mongo:8 | 27017 | Product Catalog documents |
| Dragonfly | dragonflydb | 6379 | Cache + TickerQ coordination |
| Mailpit | axllent/mailpit | 1025, 8025 | Dev email |
| Alloy | grafana/alloy | 4317 | OTLP collector |
| Prometheus | prom/prometheus | 9090 | Metrics |
| Loki | grafana/loki | 3100 | Logs |
| Tempo | grafana/tempo | 3200 | Traces |
| Grafana | grafana/grafana | 3001 | Dashboards |
| YARP Gateway | custom | 8080 | API routing |

### Running
```bash
docker compose -f docker-compose.microservices.yml up -d
```

---

## Observability

- **Tracing**: OpenTelemetry -> Alloy -> Tempo -> Grafana
- **Metrics**: OpenTelemetry -> Alloy -> Prometheus -> Grafana
- **Logging**: Serilog -> Console + OTLP -> Alloy -> Loki -> Grafana
- **Instrumentation**: ASP.NET Core, HTTP Client, Npgsql, Wolverine, Runtime, Process

---

## Key Design Decisions

1. **EF Core (not Marten)** - existing codebase uses EF Core, no migration to event sourcing
2. **REST only (no GraphQL)** - simplified API surface for microservices
3. **Notifications = A-Frame pilot** - Wolverine HTTP Endpoints instead of ASP.NET Controllers
4. **Other services = ASP.NET Controllers** - proven pattern, familiar to team
5. **Database-per-service** - separate PostgreSQL databases via init script
6. **SharedKernel DRY** - TenantAuditableDbContext base, shared service registration, common queue infrastructure
7. **Saga for cascading deletes** - replaces synchronous SoftDeleteCascadeRules
8. **ProductProjection read model** - Reviews stores denormalized product data, updated via events
