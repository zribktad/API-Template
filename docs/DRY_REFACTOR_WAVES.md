# DRY refactor — waves, checklists, verification

Companion to the **Repo DRY Full Backlog** plan. This document is the operational runbook: **Wave 1 checklist**, **PR-sized slices**, **Wave 2/3 plans**, and a **verification matrix**.

---

## Wave 1 — migration checklist (concrete order)

Use this order to minimize merge pain and keep the solution buildable after each step.

### 1. Shared cache + output-caching canonical types

1. `src/SharedKernel/SharedKernel.Application/Common/Events/CacheInvalidationNotification.cs`
2. `src/SharedKernel/SharedKernel.Application/Common/Events/CacheTags.cs`
3. `src/SharedKernel/SharedKernel.Application/Common/Events/CacheInvalidationBusExtensions.cs`
4. Remove duplicate monolith copies under `src/APITemplate.Application/Common/Events/` (cache-only types).
5. Add `ProjectReference` to `SharedKernel.Application` in `APITemplate.Application.csproj` if missing.
6. Replace `PublishAsync(new CacheInvalidationNotification(...))` with `PublishCacheInvalidationAsync` in:
   - `src/APITemplate.Application/Features/**/*Command*.cs`
   - `src/Services/*/*.Application/Features/**/*Command*.cs`
7. Align monolith API cache wiring with SharedKernel tags:
   - `src/APITemplate.Api/Api/Cache/*`
   - `src/APITemplate.Api/Extensions/ApiServiceCollectionExtensions.cs`
8. Shared output caching core:
   - `src/SharedKernel/SharedKernel.Api/OutputCaching/*`
   - `src/SharedKernel/SharedKernel.Api/Extensions/OutputCachingExtensions.cs` (if present)

### 2. Controller `ErrorOr` mapping helpers

1. `src/SharedKernel/SharedKernel.Api/Controllers/ApiControllerBase.cs`  
   - Helpers use **single** `TResponse` type arg; message parameter is `object` (avoids C# partial generic inference issues).
2. `src/APITemplate.Api/Api/Controllers/ApiControllerBase.cs` — mirror helpers; keep monolith-specific `ErrorOr` mappers under `APITemplate.Api/Api/ErrorOrMapping/`.
3. Migrate controllers incrementally:
   - `src/Services/*/*.Api/Controllers/**/*.cs`
   - `src/APITemplate.Api/Api/Controllers/V1/*.cs`
4. Preserve special flows explicitly (e.g. `JobsController` Accepted + Location).

### 3. Microservice pipeline facade

1. `src/SharedKernel/SharedKernel.Api/Extensions/WebApplicationPipelineExtensions.cs`
2. Each `Program.cs` under `src/Services/*/*.Api/` — call `UseSharedMicroserviceApiPipeline` with correct flags.

### 4. Tests after namespace moves

1. Any test referencing `CacheInvalidationNotification` / `CacheTags` → `using SharedKernel.Application.Common.Events;`
2. Example files: `tests/APITemplate.Tests/Integration/Infrastructure/WolverineNotificationTests.cs`,  
   `tests/APITemplate.Tests/Unit/Handlers/*RequestHandlersTests.cs`

### 5. Config drift (local dev parity)

Canonical local Keycloak: **`http://localhost:8180/`**, client/resource aligned with realm export **`api-template`**.

1. `src/Services/*/*.Api/appsettings.json` — `Keycloak:auth-server-url`, `Keycloak:resource`
2. `src/Gateway/Gateway.Api/appsettings.json` + `appsettings.Development.json`
3. ProductCatalog DB/Mongo names aligned with `infrastructure/docker/init-microservices-databases.sql` and `.vscode/launch.json`:
   - PostgreSQL: **`productcatalog_db`**
   - MongoDB: **`productcatalog`**
4. `src/Services/ProductCatalog/ProductCatalog.Infrastructure/Persistence/*DesignTimeFactory.cs`

### 6. OpenAPI / options cleanup (Wave 1 tail)

1. Deduplicate transformers: `src/SharedKernel/SharedKernel.Api/OpenApi/*` vs `src/APITemplate.Api/Api/OpenApi/*`
2. Normalize `AddValidatedOptions` patterns across hosts.

---

## Wave 1 — atomic PR-ready slices

| PR | Scope | Risk |
|----|--------|------|
| **W1-A** | SharedKernel cache types + `CacheInvalidationBusExtensions`; remove monolith duplicates; handler wiring | Low |
| **W1-B** | Command handler migration to `PublishCacheInvalidationAsync` (one bounded context per commit if large) | Low |
| **W1-C** | `ApiControllerBase` helpers + one pilot service (e.g. ProductCatalog) | Low |
| **W1-D** | Remaining API controllers | Low–medium |
| **W1-E** | `WebApplicationPipelineExtensions` + `Program.cs` updates | Low |
| **W1-F** | Config: Keycloak URL/resource + ProductCatalog connection strings + design-time factory | Low |
| **W1-G** | OpenAPI transformer / options binding consolidation | Medium |

---

## Wave 2 — detailed plan (medium risk)

**Epic C — Host / pipeline facades**

- Extend SharedKernel with facades for:
  - Standard middleware order: auth → authz → shared output caching → OpenAPI → health → controllers / Wolverine HTTP.
  - Bootstrap: Serilog, observability, auth + OpenAPI + output-cache authorization registration.
- Keep **Identity dual scheme** and worker-style hosts as explicit overrides (no one-size-fits-all).

**Epic D — Application orchestration**

- Resilient integration-event publish helper (`try` / `catch` + `LogWarning`) for Identity + monolith.
- Optional: unify `GetByIdOrError` + `IsError` where it improves readability.
- **Batch template** for Product create/update/delete shared between ProductCatalog and APITemplate.Application.
- ProductData image/video creation: Strategy + Factory for media type (only if duplication remains painful).

**Epic G — Design-time DbContext factories**

- Shared helper or centralized defaults for connection strings (all `*DbContextDesignTimeFactory.cs` + monolith factories).
- After changes: `dotnet ef migrations list` per context.

---

## Wave 3 — detailed plan (higher semantic risk)

**Epic E — Cross-cutting handlers**

- Webhooks delivery handler shape → generic helper.
- Notifications email enqueue → templating + queueing facade.
- Tenant deactivation handlers → structural alignment without violating BC boundaries.
- Saga helpers: `TryComplete`, timeout logging.

**Epic F — Specifications / validation**

- Shared list-specification building blocks (`ApplyFilter`, `AsNoTracking`, `ApplySort`, `Select`).
- By-id / by-ids query handler templates.
- FluentValidation shared rules (pagination, sort, ranges, roles) in extensions/base validators.

---

## Verification matrix (per wave)

| Check | Wave 1 | Wave 2 | Wave 3 |
|--------|--------|--------|--------|
| `dotnet build` (solution) | ✓ | ✓ | ✓ |
| Affected test projects (`dotnet test --filter FullyQualifiedName~…`) | ✓ | ✓ | ✓ |
| Full `dotnet test` (CI parity) | ✓ after W1 merge | ✓ | ✓ |
| Smoke: OpenAPI/Scalar + `/health` on touched APIs | ✓ | ✓ | ✓ |
| Auth: obtain token vs Keycloak **8180**, call one secured endpoint | ✓ | ✓ | ✓ |
| Output cache: tagged GET + invalidation after mutating command | ✓ | ✓ | ✓ |
| `dotnet ef` design-time / migrations for edited contexts | — | ✓ | ✓ |
| RabbitMQ / saga / integration event flows (if touched) | — | ✓ | ✓ |

---

## Guardrails (all waves)

- Do not change business rules or permission semantics.
- Introduce Facade / Strategy / Template Method only where duplication is real (YAGNI).
- Preserve bounded contexts — no domain logic leakage between services.
- Prefer: add canonical abstraction in SharedKernel (or BC-appropriate layer) → migrate call sites.
