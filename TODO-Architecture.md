# Architecture: Microservices with A-Frame + Wolverine + RabbitMQ

## Status: IMPLEMENTED

All 7 microservices extracted from the monolith and running independently.

---

## Open TODOs

### Reliability
- Replace in-memory `ChannelJobQueue` in BackgroundJobs with durable persistence/replay so queued jobs survive process restarts.
- Replace in-memory `ChannelEmailQueue` in Notifications with durable persistence/replay so pending emails are not lost on restart between enqueue and delivery.

### File Storage Cascade
- Implement real product-related file cleanup in FileStorage on `ProductDeletedIntegrationEvent`.
- Introduce an explicit relation from stored files to product-owned resources, so `FilesCascadeCompleted` is emitted only after actual cleanup instead of a placeholder acknowledgment (currently publishes `FilesCascadeCompleted` with `count = 0`).

### Messaging — Idempotency
- Add `Guid MessageId` to all integration event records and guard consumers with an idempotency check against a seen-message store (RabbitMQ redelivery can duplicate work today).

### DRY — Product validators
- Consolidate `ProductValidationRules` and `ProductRequestValidatorBase<T>` from `APITemplate.Application` and `ProductCatalog.Application` into a single shared location (e.g. `SharedKernel.Application`).

### Test Coverage
- Add integration/runtime coverage for microservices shared auth: permission policies, BFF cookie/OIDC flow (where applicable), tenant claim enrichment, and locked-down Wolverine HTTP endpoints.

### Gateway — Demo / Example APIs
- Expose monolith-style demo endpoints (`IdempotentController`, `PatchController`, `SseController` under `src/APITemplate.Api/`) via a dedicated Examples service or Gateway host, and register routes in YARP.

### Efficiency — Must Fix (pre-production)

- **RabbitMQ health check connection churn** (`SharedKernel.Messaging/HealthChecks/RabbitMqHealthCheck.cs`):
  Every health check poll (30s × 7 services) creates a brand-new TCP+AMQP connection and immediately disposes it. Reuse the existing Wolverine-managed connection or cache a single health-check connection.

- **Webhook delivery is sequential per subscriber** (`Webhooks.Infrastructure/Delivery/WebhookDeliveryService.cs:52-56`):
  `foreach` + `await` for each subscriber = N sequential HTTP round-trips. Each delivery log is also persisted individually (N separate DB writes). Use `Parallel.ForEachAsync` with bounded concurrency and batch `SaveChangesAsync` after all deliveries.

- **No PostgreSQL connection pooling configuration** (all service `Program.cs` + `docker-compose.microservices.yml`):
  7 services + Wolverine persistence = up to 700 simultaneous connections (default Npgsql pool = 100). Add `Maximum Pool Size=25;Minimum Pool Size=2;Connection Idle Lifetime=60` to connection strings.

- **Docker builds copy entire repository** (all `Dockerfile`s):
  `COPY . .` invalidates cache on any file change in any service. Split into csproj-only COPY for restore layer, then full copy for build. Use `.dockerignore` to exclude unrelated services.

### Efficiency — Should Fix

- **Gateway blocks startup until ALL 7 services healthy** (`docker-compose.microservices.yml:118-133`):
  YARP handles upstream failures gracefully. Remove `depends_on: service_healthy` conditions — let Gateway start immediately and route to healthy backends dynamically.

- **FileStorage missing TenantId index** (`FileStorage.Infrastructure/Persistence/Configurations/StoredFileConfiguration.cs`):
  No indexes beyond PK. Global query filter on `TenantId` causes full table scans. All other services have `TenantId` indexes — FileStorage is the only one missing it.

- **`MigrateDbAsync` blocks startup** (`SharedKernel.Api/Extensions/HostExtensions.cs`):
  7 services run migrations concurrently against shared PostgreSQL → lock contention. Consider advisory lock or sequential migration orchestration.

- **Sequential saga completion publishes** (`ProductCatalog.Application/EventHandlers/TenantDeactivatedEventHandler.cs:53-58`):
  Two independent `bus.PublishAsync` calls awaited sequentially. Use Wolverine's `OutgoingMessages` cascading pattern (already used elsewhere via `CacheInvalidationCascades`).

- **ChangeTracker `.ToList()` on every SaveChanges** (`SharedKernel.Infrastructure/Persistence/TenantAuditableDbContext.cs:70-76`):
  Materializes all change-tracked entries on every `SaveChangesAsync` (hot path). Needed because loop can modify collection during soft-delete, but could split into two passes: streaming for non-Delete, `.ToList()` only for Delete.

- **`FluentValidationActionFilter` uses reflection per request** (`SharedKernel.Api/Filters/Validation/FluentValidationActionFilter.cs:38`):
  `MakeGenericType` + DI lookup on every request, for every action argument. Cache resolved validators in `ConcurrentDictionary<Type, IValidator?>`.

- **`CacheInvalidationCascades.None` returns shared mutable instance** (`SharedKernel.Application/Common/Events/CacheInvalidationCascades.cs:12-13`):
  `OutgoingMessages` inherits `List<object>` — if any caller accidentally adds to the shared `None` singleton, it corrupts all consumers. Return `new OutgoingMessages()` each time or use a read-only wrapper.

### DRY — Service Registration

- **Split `AddSharedInfrastructure<TDbContext>`** (`SharedKernel.Api/Extensions/SharedServiceRegistration.cs`):
  Extract non-generic `AddSharedCoreServices()` (TimeProvider, HttpContextAccessor, context providers, error handling, versioning) so Webhooks/Notifications can call it without needing tenant-aware DbContext. The generic `AddSharedInfrastructure<TDbContext>()` then calls `AddSharedCoreServices()` plus UoW/audit/soft-delete.

- **Move `DbContext` base-type registration into `AddSharedInfrastructure<TDbContext>`**:
  `services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>())` is copy-pasted in 5 services (Identity, ProductCatalog, Reviews, FileStorage, BackgroundJobs). The generic type parameter is already available in `AddSharedInfrastructure<TDbContext>`.

- **Move `IRolePermissionMap` registration into `AddSharedAuthorization`**:
  `AddSingleton<IRolePermissionMap, DefaultRolePermissionMap>()` is duplicated in 4 services. Register it automatically when `enablePermissionPolicies: true`.

- **Automate `HasQueryFilter` for `IAuditableTenantEntity`** in `TenantAuditableDbContext.OnModelCreating`:
  The identical filter expression `(!HasTenant || e.TenantId == CurrentTenantId) && !e.IsDeleted` is copy-pasted 8× across 4 DbContexts. Scan for all entities implementing `IAuditableTenantEntity` and apply automatically. Subclasses opt out for special cases only.

- **Wolverine bootstrap helper** (`SharedKernel.Messaging`):
  Add `opts.ApplySharedWolverineDefaults(connectionString)` that bundles conventions + retry + PostgreSQL persistence + EF transactions. 5 services repeat the same 4-line preamble.

### DRY — Code Quality

- **Use existing `TenantAuditableDbContextDependencies` parameter object**:
  The record was created but never used. All 4 derived DbContexts manually forward 7 constructor parameters to `base(...)`. Switch constructors to accept the single parameter object.

- **Shared `DesignTimeDbContextFactoryBase<TContext>`** with NullObject implementations:
  4 identical DesignTime factories pass `null!` for `tenantProvider`, `actorProvider`, `entityStateManager`, `softDeleteProcessor`. Runtime NRE risk if any EF tooling path triggers `SaveChangesAsync`.

- **Collapse webhook event handlers into a generic handler** (`Webhooks.Application/Features/Delivery/EventHandlers/`):
  4 handlers (`ProductCreated`, `ProductDeleted`, `ReviewCreated`, `CategoryDeleted`) are identical: log → serialize → deliver. Extract common `ITenantEvent` interface and one generic handler.

- **Saga `NotFound` helper** (`TenantDeactivationSaga`, `ProductDeletionSaga`):
  7 identical `NotFound` methods across 2 sagas. Extract shared helper or Wolverine convention.

- **Webhooks resilience pipeline key** (`Webhooks.Api/Program.cs:53`):
  Raw string `"outgoing-webhook-retry"` — other services use typed constants. Add to a constants class.

### Architectural Consistency

- **Webhooks bypasses UnitOfWork / tenant auditing** (`Webhooks.Infrastructure/`):
  Repositories call `_dbContext.SaveChangesAsync()` directly instead of `IUnitOfWork.CommitAsync()`. Entities implement `IAuditableTenantEntity` but use plain `DbContext` → no audit stamping, no query filters, no soft-delete processing. Manual tenant filtering in queries. Risk: deleted webhook subscriptions could be served.

- **BackgroundJobs registers unused shared infrastructure** (`BackgroundJobs.Api/Program.cs:56`):
  Calls `AddSharedInfrastructure<BackgroundJobsDbContext>` but `BackgroundJobsDbContext` extends raw `DbContext`, not `TenantAuditableDbContext`. UoW, auditing, and soft-delete services are registered but never invoked.

- **Notifications skips API versioning** (`Notifications.Api/Program.cs`):
  Does not call `AddSharedInfrastructure` → misses API versioning registration that all other services get. Calls `AddSharedApiErrorHandling()` separately.

- **Redundant queue interface layer in Notifications Domain**:
  `IQueue<T>` and `IQueueReader<T>` in `Notifications.Domain.Interfaces` are empty marker interfaces re-exporting SharedKernel interfaces. `IEmailQueue`/`IEmailQueueReader` could extend SharedKernel directly (like `BackgroundJobs.Application.Common.IJobQueue` already does).

- **Reviews `ProductProjection` inconsistency**:
  Uses `IsActive` flag instead of `IsDeleted` + `AuditInfo` pattern used everywhere else. Semantically different from the rest of the system.

### Testing

- **SQLite test setup boilerplate** (`tests/Identity.Tests/`, `tests/ProductCatalog.Tests/`):
  Identical SQLite connection + DbContext + `EnsureCreated()` + `IDisposable` teardown copy-pasted across test classes. Add `SqliteTestDbContextFactory<TContext>` to `Tests.Common`.

- **Per-service `TestDbContext` duplication** (`tests/Identity.Tests/TestDbContext.cs`, `tests/ProductCatalog.Tests/TestDbContext.cs`, `tests/Reviews.Tests/TestDbContext.cs`):
  Identical `OwnsOne(Audit)` + key convention per entity. Add `TestModelBuilderExtensions.ConfigureTestAuditableEntity<T>()` to `Tests.Common`.

### Architectural Decision Review

> **Question to evaluate:** Is microservices the right architecture for this project's scale?
>
> **Evidence against:**
> - SharedKernel = 6717 LOC, all services combined = 3537 LOC (without migrations). Shared code is 1.9× larger than business code.
> - 7 services × 4 layers = 28 projects + 5 SharedKernel + 1 Contracts + 1 Gateway = 35 projects (was 5 in monolith — 7× increase).
> - All services are structurally identical (same middleware, same auth, same persistence patterns).
> - `RabbitMqTopology` is a static map in SharedKernel — changing a queue name requires coordinated deployment.
> - Saga timeouts accept partial completion silently (`MarkCompleted()` on timeout) — was a single DB transaction in the monolith.
> - Local dev requires 16+ containers vs 8 for monolith.
> - No documented drivers (independent scaling, deployment cadence, team boundaries, technology heterogeneity).
>
> **Alternative considered:** Modular monolith — same boundary enforcement (separate assemblies per bounded context, explicit integration event contracts, Wolverine as in-process bus) at a fraction of operational cost. Extract individual services later when concrete production metrics justify it.
>
> **Decision:** [TO BE EVALUATED]

### Optional polish
- **Output cache:** ProductCatalog, Identity, Reviews, and FileStorage use `AddSharedOutputCaching` + `[OutputCache]` + invalidation. BackgroundJobs still runs with `useOutputCaching: false` (it has a GET on `JobsController`); enable caching there only if you want read responses cached like the other APIs.

---

## Completed ([x])

### [x] Saga — EF Core persistence
`ProductDeletionSaga` and `TenantDeactivationSaga` are mapped in each service `DbContext`, with EF migrations and saga schema (`sagas`).

### [x] Saga — Timeout handling
Both sagas schedule Wolverine timeouts (`ProductDeletionSagaTimeout`, `TenantDeactivationSagaTimeout`) with compensation-style handling.

### [x] Output caching (microservices read APIs)
Tenant-aware policies, Redis/Dragonfly-backed output cache registration (`AddSharedOutputCaching`), `UseSharedOutputCaching` in the shared pipeline, `CacheInvalidationHandler`, and write-side `CacheInvalidationCascades` are implemented in `SharedKernel.Api` / `SharedKernel.Application` and wired for **ProductCatalog, Identity, Reviews, FileStorage** (including `[OutputCache]` on their GET controllers).

### [x] Saga-flow integration tests
`tests/Integration.Tests/Sagas/ProductDeletionSagaIntegrationTests.cs` and `TenantDeactivationSagaIntegrationTests.cs` exercise cross-service correlation and completion messages.

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
