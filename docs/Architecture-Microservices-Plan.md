# Microservices Migration Plan: A-Frame + Wolverine + RabbitMQ + DDD

## Context

The API-Template project is a well-structured Clean Architecture monolith (.NET 10) with Wolverine as in-process message bus, EF Core + PostgreSQL + MongoDB, multi-tenancy, and 7 identified bounded contexts. The goal is to transform it into independent microservices using:

- **A-Frame Architecture** (Jeremy D. Miller) with Wolverine as conductor
- **Wolverine Sagas** for cross-service orchestration
- **RabbitMQ** for inter-service messaging
- **Clean Architecture + Vertical Slice + DDD** within each service
- **YARP** API Gateway
- **EF Core** persistence (no Marten)
- **REST only** (GraphQL removed)
- **Notifications service** as Wolverine HTTP Endpoints pilot (A-Frame)
- Remaining services use ASP.NET Controllers

---

## 1. Solution Structure

```
solution/
  src/
    SharedKernel/
      SharedKernel.Domain/              # Entity contracts, value objects, exceptions
      SharedKernel.Application/         # Cross-cutting: ErrorOr middleware, batch, DTOs
      SharedKernel.Infrastructure/      # Base DbContext, audit, soft-delete, UoW, repos
      SharedKernel.Messaging/           # Wolverine conventions, RabbitMQ topology, tenant propagation
      SharedKernel.Api/                 # Base controller, ErrorOr mapping, auth, health checks

    Contracts/
      Contracts.IntegrationEvents/      # Integration event records ONLY (no domain logic)

    Services/
      ProductCatalog/
        ProductCatalog.Domain/
        ProductCatalog.Application/
        ProductCatalog.Infrastructure/
        ProductCatalog.Api/             # ASP.NET Controllers
      Reviews/
        Reviews.Domain/
        Reviews.Application/
        Reviews.Infrastructure/
        Reviews.Api/                    # ASP.NET Controllers
      Identity/
        Identity.Domain/
        Identity.Application/
        Identity.Infrastructure/
        Identity.Api/                   # ASP.NET Controllers
      Notifications/
        Notifications.Domain/
        Notifications.Application/
        Notifications.Infrastructure/
        Notifications.Api/              # Wolverine HTTP Endpoints (A-Frame pilot)
      FileStorage/
        FileStorage.Domain/
        FileStorage.Application/
        FileStorage.Infrastructure/
        FileStorage.Api/                # ASP.NET Controllers
      BackgroundJobs/
        BackgroundJobs.Domain/
        BackgroundJobs.Application/
        BackgroundJobs.Infrastructure/
        BackgroundJobs.Api/             # ASP.NET Controllers
      Webhooks/
        Webhooks.Domain/
        Webhooks.Application/
        Webhooks.Infrastructure/
        Webhooks.Api/                   # ASP.NET Controllers

    Gateway/
      Gateway.Api/                      # YARP reverse proxy

  tests/
    SharedKernel.Tests/
    ProductCatalog.Tests/
    Reviews.Tests/
    Identity.Tests/
    Notifications.Tests/
    FileStorage.Tests/
    BackgroundJobs.Tests/
    Webhooks.Tests/
    Integration.Tests/                  # Cross-service E2E tests

  infrastructure/
    docker/
    observability/
```

### Project Reference Rules

- **Domain** -> SharedKernel.Domain only
- **Application** -> Domain, SharedKernel.Application, Contracts.IntegrationEvents
- **Infrastructure** -> Application, Domain, SharedKernel.Infrastructure
- **Api** -> Application, Infrastructure, SharedKernel.Api, SharedKernel.Messaging
- **No service references another service's projects** - only Contracts.IntegrationEvents

---

## 2. A-Frame Architecture Pattern (Per Service)

Three functional areas in each handler:

| Area | Role | Who |
|------|------|-----|
| **Load** (Infrastructure) | Fetch data, validate existence | Static `LoadAsync` method |
| **Handle** (Domain Logic) | Pure business decisions | Static `Handle`/`HandleAsync` method |
| **Conductor** | Orchestrates Load -> Handle -> side effects | Wolverine (generated) |

### Example: Delete Products Handler (A-Frame)

```csharp
// ProductCatalog.Application/Features/Products/Commands/DeleteProducts/DeleteProductsHandler.cs
public static class DeleteProductsHandler
{
    // LOAD: Infrastructure concern
    public static async Task<(HandlerContinuation, IReadOnlyList<Product>?)> LoadAsync(
        DeleteProductsCommand command,
        IProductRepository repository,
        CancellationToken ct)
    {
        IReadOnlyList<Product> products = await repository.ListAsync(
            new ProductsByIdsSpec(command.Ids.ToHashSet()), ct);
        if (products.Count != command.Ids.Count)
            return (HandlerContinuation.Stop, null);
        return (HandlerContinuation.Continue, products);
    }

    // HANDLE: Pure domain logic - returns response + integration event
    public static (BatchResponse, ProductDeletedIntegrationEvent) Handle(
        DeleteProductsCommand command,
        IReadOnlyList<Product> products)
    {
        foreach (Product product in products)
            product.SoftDeleteProductDataLinks();
        return (
            new BatchResponse([], command.Ids.Count, 0),
            new ProductDeletedIntegrationEvent(products.Select(p => p.Id).ToList())
        );
    }
}
```

### Vertical Slice within Clean Architecture

Each feature is a vertical slice (co-located command, handler, validator, DTOs) inside the Application layer. Clean Architecture layers remain project boundaries enforcing dependency direction.

```
Application/Features/Products/Commands/CreateProducts/
  CreateProductsCommand.cs       # Message record
  CreateProductsHandler.cs       # A-Frame Load + Handle
  CreateProductsValidator.cs     # FluentValidation
  CreateProductsResponse.cs      # DTO
```

---

## 3. SharedKernel Contents

### SharedKernel.Domain
*Migrated from `APITemplate.Domain/Entities/Contracts/` and `APITemplate.Domain/Exceptions/`*

- `IAuditableTenantEntity`, `IAuditableEntity`, `ISoftDeletable`, `IHasId`, `ITenantEntity`
- `AuditInfo` value object, `AuditDefaults`
- `PagedResponse<T>`
- `IRepository<T>` base (Ardalis)
- `IUnitOfWork`
- Domain exceptions: `AppException`, `NotFoundException`, `ValidationException`, `ConflictException`

### SharedKernel.Application
*Migrated from `APITemplate.Application/Common/`*

- `ITenantProvider`, `IActorProvider`
- `BatchResponse`, `BatchFailureContext<T>`, batch rules
- `ErrorOrValidationMiddleware`
- `ErrorCatalog` (general section)
- Shared validation, sorting, search helpers

### SharedKernel.Infrastructure
*Migrated from `APITemplate.Infrastructure/Persistence/` and `APITemplate.Infrastructure/Repositories/`*

- **`SharedDbContext`** base class with tenant filters, audit stamping, soft-delete processing
- `AuditableEntityStateManager`
- `ISoftDeleteCascadeRule`, `SoftDeleteProcessor`
- `UnitOfWork`, `EfCoreTransactionProvider`, `ManagedTransactionScope`
- `RepositoryBase<T>`
- Pagination helpers
- `IIdempotencyStore`, `DistributedCacheIdempotencyStore`
- OpenTelemetry + Serilog registration helpers

### SharedKernel.Messaging
- Wolverine convention registration (handler discovery, middleware policy)
- RabbitMQ topology conventions (exchange/queue naming)
- `TenantAwareEnvelopeMapper` (propagates TenantId via `x-tenant-id` header)
- Outbox/inbox policy registration
- Retry/error handling policy defaults

### SharedKernel.Api
- `ApiControllerBase` with ErrorOr helpers
- `ToActionResult`, `ToBatchResult` extensions
- Global `ProblemDetails` exception handler
- `RequirePermissionAttribute`, Permission constants
- Idempotency filter
- Health check registration
- Rate limiting, CORS, OpenAPI helpers

---

## 4. Integration Events (Contracts.IntegrationEvents)

```csharp
// Identity events
public sealed record UserRegisteredIntegrationEvent(Guid UserId, Guid TenantId, string Email, string Username, DateTime OccurredAtUtc);
public sealed record UserRoleChangedIntegrationEvent(Guid UserId, Guid TenantId, string Email, string Username, string OldRole, string NewRole, DateTime OccurredAtUtc);
public sealed record TenantDeactivatedIntegrationEvent(Guid TenantId, Guid ActorId, DateTime OccurredAtUtc);
public sealed record TenantInvitationCreatedIntegrationEvent(Guid InvitationId, string Email, string TenantName, string Token, DateTime OccurredAtUtc);

// Product Catalog events
public sealed record ProductCreatedIntegrationEvent(Guid ProductId, Guid TenantId, string Name, DateTime OccurredAtUtc);
public sealed record ProductDeletedIntegrationEvent(IReadOnlyList<Guid> ProductIds, Guid TenantId, DateTime OccurredAtUtc);
public sealed record CategoryDeletedIntegrationEvent(Guid CategoryId, Guid TenantId, DateTime OccurredAtUtc);

// Reviews events
public sealed record ReviewCreatedIntegrationEvent(Guid ReviewId, Guid ProductId, Guid UserId, Guid TenantId, int Rating, DateTime OccurredAtUtc);

// Saga responses
public sealed record ReviewsCascadeCompleted(Guid CorrelationId, int DeletedCount);
public sealed record FilesCascadeCompleted(Guid CorrelationId, int DeletedCount);
public sealed record ProductsCascadeCompleted(Guid CorrelationId, Guid TenantId, int DeletedCount);
public sealed record UsersCascadeCompleted(Guid CorrelationId, Guid TenantId, int DeactivatedCount);
public sealed record CategoriesCascadeCompleted(Guid CorrelationId, Guid TenantId, int DeletedCount);
```

---

## 5. RabbitMQ Messaging Topology

### Exchanges (topic, durable)

| Exchange | Publisher | Routing Keys |
|----------|-----------|-------------|
| `identity.events` | Identity | `user.registered`, `user.role-changed`, `tenant.deactivated`, `tenant.invitation.created` |
| `product-catalog.events` | Product Catalog | `product.created`, `product.deleted`, `category.deleted` |
| `reviews.events` | Reviews | `review.created` |

### Queue Bindings

| Queue | Exchange | Routing Key | Consumer |
|-------|----------|------------|----------|
| `reviews.product-deleted` | product-catalog.events | `product.deleted` | Reviews |
| `reviews.tenant-deactivated` | identity.events | `tenant.deactivated` | Reviews |
| `file-storage.product-deleted` | product-catalog.events | `product.deleted` | File Storage |
| `notifications.user-registered` | identity.events | `user.registered` | Notifications |
| `notifications.user-role-changed` | identity.events | `user.role-changed` | Notifications |
| `notifications.invitation-created` | identity.events | `tenant.invitation.*` | Notifications |
| `product-catalog.tenant-deactivated` | identity.events | `tenant.deactivated` | Product Catalog |
| `webhooks.product-created` | product-catalog.events | `product.created` | Webhooks |
| `background-jobs.tenant-deactivated` | identity.events | `tenant.deactivated` | Background Jobs |

### Dead Letter

All queues: `x-dead-letter-exchange: dlx.default` -> `dlq.default`

### Wolverine RabbitMQ Config (per service)

```csharp
opts.UseRabbitMq(rabbit => { rabbit.HostName = "rabbitmq"; })
    .AutoProvision()
    .UseConventionalRouting();
opts.PersistMessagesWithPostgresql(connectionString); // Outbox in service DB
opts.Policies.UseDurableInboxOnAllListeners();
opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
```

### Required NuGet Packages per Service

```xml
<!-- Directory.Packages.props additions -->
<PackageVersion Include="WolverineFx.RabbitMQ" Version="5.22.0" />
<PackageVersion Include="WolverineFx.EntityFrameworkCore" Version="5.22.0" />
<PackageVersion Include="WolverineFx.Http" Version="5.22.0" />  <!-- Notifications only -->
<PackageVersion Include="Yarp.ReverseProxy" Version="2.3.0" />  <!-- Gateway only -->
```

---

## 6. Wolverine Saga Patterns

### 6.1 Product Deletion Cascade Saga (hosted in Product Catalog)

Replaces `ProductSoftDeleteCascadeRule` which currently queries Reviews + ProductDataLinks in same DbContext.

```csharp
public class ProductDeletionSaga : Saga
{
    public string? Id { get; set; }
    public IReadOnlyList<Guid> ProductIds { get; set; } = [];
    public Guid TenantId { get; set; }
    public bool ReviewsCascaded { get; set; }
    public bool FilesCascaded { get; set; }

    public static (ProductDeletionSaga, SagaTimeout) Start(StartProductDeletionSaga command)
    {
        return (new ProductDeletionSaga
        {
            Id = command.CorrelationId.ToString(),
            ProductIds = command.ProductIds,
            TenantId = command.TenantId
        }, new SagaTimeout(command.CorrelationId));
    }

    public void Handle(ReviewsCascadeCompleted _) { ReviewsCascaded = true; TryComplete(); }
    public void Handle(FilesCascadeCompleted _) { FilesCascaded = true; TryComplete(); }
    public void Handle(SagaTimeout _) => MarkCompleted(); // Accept partial on timeout

    private void TryComplete()
    {
        if (ReviewsCascaded && FilesCascaded) MarkCompleted();
    }
}
```

### 6.2 Tenant Deactivation Cascade Saga (hosted in Identity)

Replaces `TenantSoftDeleteCascadeRule` which queries Users, Products, Categories in same DbContext.

```csharp
public class TenantDeactivationSaga : Saga
{
    public string? Id { get; set; }
    public Guid TenantId { get; set; }
    public bool UsersCascaded { get; set; }
    public bool ProductsCascaded { get; set; }
    public bool CategoriesCascaded { get; set; }

    public static (TenantDeactivationSaga, SagaTimeout) Start(StartTenantDeactivationSaga command)
        => (new TenantDeactivationSaga { Id = command.CorrelationId.ToString(), TenantId = command.TenantId },
            new SagaTimeout(command.CorrelationId));

    public void Handle(UsersCascadeCompleted _) { UsersCascaded = true; TryComplete(); }
    public void Handle(ProductsCascadeCompleted _) { ProductsCascaded = true; TryComplete(); }
    public void Handle(CategoriesCascadeCompleted _) { CategoriesCascaded = true; TryComplete(); }
    public void Handle(SagaTimeout _) => MarkCompleted();

    private void TryComplete()
    {
        if (UsersCascaded && ProductsCascaded && CategoriesCascaded) MarkCompleted();
    }
}
```

### 6.3 Choreography (no saga needed)

| Workflow | Pattern | Why |
|----------|---------|-----|
| User registration -> welcome email | Choreography | Single consumer, fire-and-forget |
| Tenant invitation -> invitation email | Choreography | Single consumer, fire-and-forget |
| Role change -> notification | Choreography | Single consumer, fire-and-forget |
| Review creation -> product validation | Local read projection | Reviews stores `ProductProjection` table updated via events |

### Saga persistence

Two verified options:
- **Option A (Recommended)**: `WolverineFx.EntityFrameworkCore` - saga state mapped as entity in service's DbContext. Full EF Core integration, same transaction as business data.
- **Option B**: `WolverineFx.Postgresql` lightweight saga storage - JSON serialized saga state in auto-created `{SagaName}_saga` tables. No DbContext mapping needed.

---

## 7. Notifications Service (A-Frame Pilot - Wolverine HTTP Endpoints)

### Domain

```
Notifications.Domain/
  Entities/FailedEmail.cs         # Migrated
  Interfaces/IFailedEmailRepository.cs
```

### Application (Wolverine HTTP Endpoints)

```csharp
// GET /api/v1/notifications/failed-emails
public static class GetFailedEmailsEndpoint
{
    [WolverineGet("/api/v1/notifications/failed-emails")]
    public static async Task<PagedResponse<FailedEmailDto>> HandleAsync(
        [FromQuery] PaginationFilter filter,
        IFailedEmailRepository repository,
        CancellationToken ct)
    {
        return await repository.GetPagedAsync(/* ... */);
    }
}

// POST /api/v1/notifications/failed-emails/{id}/retry (A-Frame Load + Handle)
public static class RetryFailedEmailEndpoint
{
    // LOAD
    public static async Task<(ProblemDetails?, FailedEmail?)> LoadAsync(
        [FromRoute] Guid id,
        IFailedEmailRepository repository,
        CancellationToken ct)
    {
        FailedEmail? email = await repository.GetByIdAsync(id, ct);
        return email is null
            ? (new ProblemDetails { Status = 404, Detail = $"Email {id} not found" }, null)
            : (WolverineContinue.NoProblems, email);
    }

    // HANDLE
    [WolverinePost("/api/v1/notifications/failed-emails/{id}/retry")]
    public static RetryEmailCommand Handle([FromRoute] Guid id, FailedEmail email)
    {
        return new RetryEmailCommand(email.Id, email.To, email.Subject, email.HtmlBody);
    }
}
```

### Event Consumers (from RabbitMQ)

```csharp
public static class UserRegisteredNotificationHandler
{
    public static async Task HandleAsync(
        UserRegisteredIntegrationEvent @event,
        IEmailTemplateRenderer renderer,
        IEmailQueue queue,
        CancellationToken ct)
    {
        string html = await renderer.RenderAsync("user-registration", new { @event.Username }, ct);
        await queue.EnqueueAsync(new EmailMessage(@event.Email, "Welcome!", html), ct);
    }
}
```

### Infrastructure

Migrated from monolith: `MailKit` sender, `Fluid` template renderer, email queue, failed email store, `NotificationsDbContext`.

### Program.cs

```csharp
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(UserRegisteredNotificationHandler).Assembly);
    opts.UseRabbitMq(r => { r.HostName = "rabbitmq"; }).AutoProvision().UseConventionalRouting();
    opts.PersistMessagesWithPostgresql(connectionString);
    opts.Policies.UseDurableInboxOnAllListeners();
    opts.ListenToRabbitQueue("notifications.user-registered");
    opts.ListenToRabbitQueue("notifications.user-role-changed");
    opts.ListenToRabbitQueue("notifications.invitation-created");
});
app.MapWolverineEndpoints(); // No MapControllers
```

---

## 8. YARP API Gateway

### Gateway.Api/Program.cs

```csharp
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
app.UseRateLimiter();
app.MapReverseProxy();
app.MapHealthChecks("/health");
```

### Routes (appsettings.json)

| Route Pattern | Cluster |
|---------------|---------|
| `/api/v1/products/**` | product-catalog:8080 |
| `/api/v1/categories/**` | product-catalog:8080 |
| `/api/v1/productreviews/**` | reviews:8080 |
| `/api/v1/users/**`, `/api/v1/tenants/**`, `/api/v1/tenantinvitations/**` | identity:8080 |
| `/api/v1/notifications/**` | notifications:8080 |
| `/api/v1/files/**` | file-storage:8080 |
| `/api/v1/jobs/**` | background-jobs:8080 |
| `/api/v1/webhooks/**` | webhooks:8080 |

Gateway handles: routing, rate limiting, CORS, correlation IDs, health aggregation. No business logic.

---

## 9. Database-per-Service

| Service | Database | Special |
|---------|----------|---------|
| Product Catalog | `productcatalog_db` | + MongoDB for ProductData |
| Reviews | `reviews_db` | + `ProductProjection` read model |
| Identity | `identity_db` | + Keycloak |
| Notifications | `notifications_db` | |
| File Storage | `filestorage_db` | + filesystem/S3 |
| Background Jobs | `backgroundjobs_db` | + TickerQ tables |
| Webhooks | `webhooks_db` | |

Each DB also contains Wolverine envelope tables (outbox/inbox) via `WolverineFx.EntityFrameworkCore`.

Reviews service maintains `ProductProjection(ProductId, TenantId, Name, IsActive)` updated via `ProductCreatedIntegrationEvent` / `ProductDeletedIntegrationEvent` - replaces direct FK to Products.

---

## 10. Error Handling & Retry

```csharp
// Per-service Wolverine config
opts.Policies.Failures
    .Handle<DbUpdateException>()
    .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds())
    .Then.MoveToErrorQueue();

opts.Policies.Failures
    .Handle<DbUpdateConcurrencyException>()
    .RetryWithCooldown(100.Milliseconds(), 500.Milliseconds())
    .Then.MoveToErrorQueue();

opts.Policies.Failures
    .Handle<TimeoutException>()
    .ScheduleRetry(5.Seconds(), 30.Seconds(), 2.Minutes())
    .Then.MoveToErrorQueue();
```

- **Idempotency**: Wolverine durable inbox (dedup by envelope ID) + existing `IIdempotencyStore` for non-naturally-idempotent ops
- **Saga timeouts**: 5-minute `SagaTimeout` message, accept partial completion
- **Dead letters**: `dlx.default` -> `dlq.default`, Wolverine `wolverine_dead_letters` table

---

## 11. Docker Compose

New services added to existing compose:

```yaml
services:
  rabbitmq:
    image: rabbitmq:4-management
    ports: ["5672:5672", "15672:15672"]

  postgres:  # Shared instance, init.sql creates per-service databases
    volumes:
      - ./infrastructure/docker/init-databases.sql:/docker-entrypoint-initdb.d/init.sql

  gateway:
    build: { dockerfile: src/Gateway/Gateway.Api/Dockerfile }
    ports: ["8080:8080"]

  product-catalog:
    build: { dockerfile: src/Services/ProductCatalog/ProductCatalog.Api/Dockerfile }
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=productcatalog_db;..."
      RabbitMQ__HostName: rabbitmq

  reviews:
    build: { dockerfile: src/Services/Reviews/Reviews.Api/Dockerfile }
  identity:
    build: { dockerfile: src/Services/Identity/Identity.Api/Dockerfile }
  notifications:
    build: { dockerfile: src/Services/Notifications/Notifications.Api/Dockerfile }
  file-storage:
    build: { dockerfile: src/Services/FileStorage/FileStorage.Api/Dockerfile }
  background-jobs:
    build: { dockerfile: src/Services/BackgroundJobs/BackgroundJobs.Api/Dockerfile }
  webhooks:
    build: { dockerfile: src/Services/Webhooks/Webhooks.Api/Dockerfile }

  # Existing: mongodb, keycloak, dragonfly, mailpit, alloy, prometheus, loki, tempo, grafana
```

---

## 12. Feasibility Assessment (Verified Against Docs + Codebase)

### Required New NuGet Packages

| Package | Purpose | Verified |
|---------|---------|----------|
| `WolverineFx.RabbitMQ` | RabbitMQ transport | Yes - `UseRabbitMq()`, `AutoProvision()`, `UseConventionalRouting()` confirmed in docs |
| `WolverineFx.Http` | Wolverine HTTP Endpoints | Yes - `[WolverineGet]`, `[WolverinePost]`, `MapWolverineEndpoints()` confirmed in docs |
| `WolverineFx.EntityFrameworkCore` | EF Core integration + saga persistence | Yes - `UseEntityFrameworkCoreTransactions()`, saga state via DbContext mapping confirmed |
| `Yarp.ReverseProxy` | API Gateway | Yes - Microsoft official package, stable |

### Current Packages (already referenced in `Directory.Packages.props`)

- `WolverineFx` v5.22.0
- `WolverineFx.FluentValidation` v5.22.0

### Component Feasibility

| Component | Feasibility | Verification Source | Details |
|-----------|------------|---------------------|---------|
| **A-Frame with Wolverine** | **HIGH** | Codebase + Jeremy D. Miller blog | Monolith already uses static Wolverine handlers (`CreateProductsCommandHandler`, `CacheInvalidationHandler`). Load/Handle separation is a naming/structure convention on top of existing pattern. No new library needed. |
| **Wolverine HTTP Endpoints** | **HIGH** | wolverine.netlify.app/guide/http/ | `[WolverineGet]`/`[WolverinePost]` documented with full Load/Handle pattern. ProblemDetails natively supported. `WolverineHttp` adds `AddWolverineHttp()` + `MapWolverineEndpoints()`. Pilot only (Notifications). |
| **Wolverine Sagas with EF Core** | **HIGH** | wolverine.netlify.app/guide/durability/sagas + /efcore | Saga persistence supports EF Core, PostgreSQL, SQL Server, SQLite, MySQL, RavenDb, CosmosDB, Oracle. For EF Core: DbContext must have mapping for saga state entity. `WolverineFx.EntityFrameworkCore` package. Lightweight saga storage (JSON in DB) also available via `WolverineFx.Postgresql`. |
| **RabbitMQ Transport** | **HIGH** | wolverine.netlify.app/guide/messaging/transports/rabbitmq | `UseRabbitMq(Uri)`, `UseRabbitMqUsingNamedConnection()`, `AutoProvision()`, `ListenToRabbitQueue()`, `PublishMessage<T>().ToRabbitExchange()`. Channel config, control queues, partitioning all documented. |
| **Outbox with EF Core (no Marten)** | **HIGH** | wolverine.netlify.app/guide/durability/efcore | `WolverineFx.EntityFrameworkCore` provides outbox integrated with EF Core's `SaveChangesAsync()`. Wolverine "both calls DbContext.SaveChangesAsync() and flushes persisted messages" in same transaction. Limitation: only one database registration per outbox. |
| **Tenant Propagation via RabbitMQ** | **HIGH** | Wolverine docs (envelope mapper) | Custom `IRabbitMqEnvelopeMapper` allows header injection/extraction. TenantId via `x-tenant-id` header. |
| **YARP Gateway** | **HIGH** | Microsoft official | Config-driven reverse proxy. Production-ready, actively maintained. |
| **SharedKernel extraction** | **HIGH** | Codebase analysis | All shared code exists in monolith under well-defined paths. Extraction = move files + update project references. |
| **Database-per-service** | **MEDIUM** | Architectural pattern | Fresh Code-First migrations per service. Reviews FK->Products replaced by `ProductProjection` read model (eventual consistency). Risk: data migration scripts needed for existing data. |
| **UnitOfWork + Wolverine outbox** | **MEDIUM** | Codebase + Wolverine docs | Current `UnitOfWork` uses `DbContext.Database.BeginTransactionAsync()`. Wolverine's EF Core middleware manages its own transaction. Rule: handlers publishing events use Wolverine transaction; internal-only handlers use custom UnitOfWork. No conflict if separated. |
| **TickerQ per-service** | **MEDIUM** | Codebase (TickerQ v10.2.2) | Each service needing schedules embeds own TickerQ instance. Background Jobs service centralizes cross-cutting jobs only. |
| **GraphQL removal** | **HIGH** | Greenfield project | Remove HotChocolate 15.1 packages + delete `Api/GraphQL/` folder. No legacy clients to break. |

### Potential Blockers

| Blocker | Severity | Resolution |
|---------|----------|------------|
| Wolverine outbox supports only 1 DB registration | LOW | Each microservice has exactly 1 database - not an issue |
| Saga state serialization for complex types (`IReadOnlyList<Guid>`) | LOW | Wolverine uses System.Text.Json for lightweight saga storage - works with standard types |
| `CritterStackDefaults` with `TypeLoadMode.Static` requires code generation | LOW | Pre-generate Wolverine code per service at build time (`dotnet build` generates) |
| Current `MessageBusExtensions.PublishSafeAsync` swallows errors | LOW | Replace with outbox pattern - errors handled by retry/dead-letter, not swallowed |

---

## 13. Risk Analysis

| Risk | Impact | Mitigation |
|------|--------|------------|
| Data inconsistency during saga | HIGH | Wolverine durable outbox (at-least-once) + idempotent handlers + saga timeouts |
| Operational complexity increase | HIGH | Shared observability (OpenTelemetry + Grafana already configured), Docker Compose for dev |
| Reviews needs Product data | MEDIUM | `ProductProjection` read model updated via integration events. Eventual consistency acceptable. |
| Two transaction managers | MEDIUM | Clear rule: outbox-publishing handlers use Wolverine transaction, internal handlers use UnitOfWork |
| Migration effort | HIGH | Phased approach - Notifications first validates patterns before committing all services |
| Network partitions | MEDIUM | Durable outbox persists locally, delivered when RabbitMQ recovers |

---

## 14. Implementation Phases

### Phase 1: Foundation
1. Create solution structure with all project shells
2. Extract SharedKernel from monolith
3. Create Contracts.IntegrationEvents
4. Set up Gateway.Api with YARP

### Phase 2: Notifications Service (A-Frame Pilot)
5. Create Notifications service with Wolverine HTTP Endpoints
6. Migrate email infrastructure (MailKit, Fluid, queue, templates)
7. Configure RabbitMQ transport + outbox
8. E2E test: Identity -> RabbitMQ -> Notifications -> email sent

### Phase 3: Identity & Tenancy
9. Create Identity service with own DbContext
10. Migrate Keycloak integration
11. Implement TenantDeactivation saga
12. Publish integration events for user/tenant lifecycle

### Phase 4: File Storage + Webhooks
13. Create File Storage service (simple CRUD)
14. Create Webhooks service (HMAC validation, delivery)

### Phase 5: Reviews
15. Create Reviews service with own DbContext
16. Add ProductProjection read model
17. Handle cascade via ProductDeletedIntegrationEvent

### Phase 6: Product Catalog
18. Create Product Catalog service (core domain, last extracted)
19. Own DbContext + MongoDB connection
20. Implement ProductDeletion saga

### Phase 7: Background Jobs
21. Create Background Jobs service with TickerQ
22. Centralized scheduling for cross-service jobs

### Phase 8: Cleanup
23. Remove old monolith projects
24. Remove GraphQL (HotChocolate)
25. E2E integration tests through YARP gateway
26. Load testing

---

## 15. Key Files to Modify/Migrate

| Source (Monolith) | Target |
|-------------------|--------|
| `src/APITemplate.Domain/Entities/Contracts/` | SharedKernel.Domain |
| `src/APITemplate.Application/Common/Middleware/ErrorOrValidationMiddleware.cs` | SharedKernel.Application |
| `src/APITemplate.Application/Common/Events/EmailEvents.cs` | Contracts.IntegrationEvents (expanded) |
| `src/APITemplate.Application/Common/Events/SoftDeleteEvents.cs` | Contracts.IntegrationEvents (expanded) |
| `src/APITemplate.Infrastructure/Persistence/AppDbContext.cs` | Decomposed into per-service DbContexts inheriting SharedDbContext |
| `src/APITemplate.Infrastructure/Persistence/SoftDelete/ProductSoftDeleteCascadeRule.cs` | Replaced by ProductDeletionSaga |
| `src/APITemplate.Infrastructure/Persistence/SoftDelete/TenantSoftDeleteCascadeRule.cs` | Replaced by TenantDeactivationSaga |
| `src/APITemplate.Infrastructure/Repositories/RepositoryBase.cs` | SharedKernel.Infrastructure |
| `src/APITemplate.Infrastructure/Persistence/UnitOfWork/` | SharedKernel.Infrastructure |
| `src/APITemplate.Infrastructure/Persistence/Auditing/` | SharedKernel.Infrastructure |
| `src/APITemplate.Infrastructure/Email/` | Notifications.Infrastructure |
| `src/APITemplate.Api/Api/Controllers/V1/` | Per-service Api projects |
| `src/APITemplate.Api/Api/GraphQL/` | **DELETED** |
| `src/APITemplate.Api/Program.cs` | Per-service Program.cs + Gateway Program.cs |

---

## 16. Verification Plan

1. **Unit tests**: A-Frame Handle methods are pure functions -> test with simple assertions
2. **Integration tests per service**: `WebApplicationFactory` + Testcontainers (PostgreSQL + RabbitMQ)
3. **Cross-service E2E**: Docker Compose up all services + Gateway, run Saga scenarios
4. **Specific scenarios to verify**:
   - Product deletion -> Reviews cascade via saga -> confirmation
   - Tenant deactivation -> all services cascade -> saga completion
   - User registration -> Notifications receives event -> email sent
   - YARP routes all endpoints correctly
   - Tenant propagation via RabbitMQ headers
   - Outbox delivery after RabbitMQ recovery
   - Dead letter handling on repeated failures
5. **Build**: `dotnet build` all projects, ensure no cross-service references
6. **Existing tests**: Adapt to per-service structure, ensure parity
