# Monolith -> Microservices Migration Gaps

What the old monolith (APITemplate.*) had that microservices still need, including how each feature was implemented.

## High Impact - Core Cross-Cutting Concerns

### 1. CORS
- No CORS config in any microservice or Gateway
- **Recommendation:** Configure at Gateway level (YARP)
- **How it was implemented:**
  Default CORS policy in `AuthenticationServiceCollectionExtensions.AddAuthenticationOptions()`. Options class `CorsOptions` with `string[] AllowedOrigins` bound from `Cors:AllowedOrigins`. Policy: `WithOrigins(...).AllowAnyHeader().AllowAnyMethod().AllowCredentials()`. Applied via `app.UseCors()` inside `UseSecurityPipeline()`.

### 2. Rate Limiting
- No rate limiting in any microservice
- **Recommendation:** Add to Gateway or per-service via SharedKernel
- **How it was implemented:**
  Fixed-window per-client rate limiter in `ApiServiceCollectionExtensions.AddRateLimiting()`. Options class `RateLimitingOptions` (`PermitLimit=100`, `WindowMinutes=1`) bound from `RateLimiting:Fixed`. Partition key: JWT `Identity.Name` > `RemoteIpAddress` > `"anonymous"`. Used `IConfigureOptions<RateLimiterOptions>` for deferred resolution. Recorded rejections via `ApiMetrics.RecordRateLimitRejection()`. Applied with `app.UseRateLimiter()` and `.RequireRateLimiting()` on `MapControllers()`/`MapGraphQL()`.

### 3. CSRF Validation
- Identity service has BFF auth but no CSRF middleware
- **Recommendation:** Add to Identity.Api pipeline
- **How it was implemented:**
  Custom `CsrfValidationMiddleware` (primary constructor with `RequestDelegate` + `IProblemDetailsService`). Skipped safe methods (GET/HEAD/OPTIONS) and Bearer-authenticated requests. For cookie-authenticated mutations, required `X-CSRF: 1` header (constants in `AuthConstants.Csrf.HeaderName`/`HeaderValue`). Returned RFC 7807 ProblemDetails 403 on failure. Ran after `UseAuthentication()`, before `UseAuthorization()`.

### 4. Request Context Middleware
- Not present in SharedKernel pipeline or any service
- **Recommendation:** Add to SharedKernel.Api pipeline
- **How it was implemented:**
  `RequestContextMiddleware` resolved correlation ID from `X-Correlation-Id` header (fallback: `TraceIdentifier`). Emitted `X-Correlation-Id`, `X-Trace-Id`, `X-Elapsed-Ms` response headers. Enriched Serilog via `LogContext.PushProperty` (CorrelationId, TenantId). Tagged `IHttpMetricsTagsFeature` with `api.surface` and `authenticated`. Constants in `RequestContextConstants`.

### 5. Serilog Request Logging Pipeline
- Microservices have basic `UseSharedSerilog` but no request-level pipeline
- **Recommendation:** Enhance SharedKernel Serilog setup
- **How it was implemented:**
  `UseRequestContextPipeline()` chained `UseMiddleware<RequestContextMiddleware>()` then `UseSerilogRequestLogging()`. Custom `GetLevel`: client-aborted = Info, 500+ = Error, 400+ = Warning, else Info. Enriched `DiagnosticContext` with `RequestHost`/`RequestScheme`. Also used `ActivityTraceEnricher` for trace/span ID enrichment and `AddApplicationRedaction()` for PII masking (HMAC for sensitive data, erasing for personal). Serilog sinks included `Serilog.Sinks.OpenTelemetry` with gRPC protocol.

### 6. Keycloak Readiness Check
- No microservice waits for Keycloak
- **Recommendation:** Add to services that depend on Keycloak (Identity, any with JWT)
- **How it was implemented:**
  `WaitForKeycloakAsync()` extension on `WebApplication`. Read `KeycloakOptions` (AuthServerUrl, Realm, SkipReadinessCheck). Built OIDC discovery URL via `KeycloakUrlHelper.BuildDiscoveryUrl()`. Used Polly `ResiliencePipeline` keyed `ResiliencePipelineKeys.KeycloakReadiness` for retry. HTTP GET to discovery endpoint with 5s timeout. Threw `InvalidOperationException` after `KeycloakOptions.ReadinessMaxRetries` retries. Wrapped in `StartupTelemetry.WaitForKeycloakReadinessAsync()`.

### 7. Distributed BFF Session Persistence
- Identity has BFF cookies but no distributed session store
- **Recommendation:** Add to Identity.Api
- **How it was implemented:**
  **ValkeyTicketStore** (was DragonflyTicketStore): implemented `ITicketStore`. Stored ASP.NET Core `AuthenticationTicket` in `IDistributedCache` (Redis/Valkey). Key prefix: `"bff:ticket:"` + GUID. Encrypted ticket bytes via `IDataProtector` (purpose: `"bff:ticket"`). TTL from `BffOptions.SessionTimeoutMinutes`. Handled `CryptographicException` gracefully on retrieve.
  **CookieSessionRefresher**: static class with `OnValidatePrincipal` callback for `CookieAuthenticationOptions.Events`. Checked token expiry against `BffOptions.TokenRefreshThresholdMinutes`. Sent refresh_token grant to Keycloak token endpoint via `IHttpClientFactory` (client: `AuthConstants.HttpClients.KeycloakToken`). Updated cookie tokens on success, rejected principal on failure. Recorded telemetry via `AuthTelemetry`.

## Medium Impact - Feature Gaps

### 8. GraphQL
- Not present in any microservice
- **Recommendation:** If needed, add to ProductCatalog.Api or a dedicated GraphQL gateway
- **How it was implemented:**
  HotChocolate `AddGraphQLServer()`. Schema: `AddQueryType<ProductQueries>()` with extensions `CategoryQueries`, `ProductReviewQueries`. Mutations: `AddMutationType<ProductMutations>()` with extension `ProductReviewMutations`. Custom types: `ProductType`, `ProductReviewType`. DataLoaders: `ProductReviewsByProductDataLoader`. Instrumentation: `GraphQlExecutionMetricsListener` (diagnostic event listener). Pagination: `MaxPageSize` from `PaginationFilter.MaxPageSize`, `IncludeTotalCount=true`. Security: `AddAuthorization()`. Depth limit: `AddMaxExecutionDepthRule(5)`. Mapped via `app.MapGraphQL()` with rate limiting + `MapNitroApp("/graphql/ui")`.

### 9. Observability / Telemetry (12 files)
- Microservices have basic `AddSharedObservability` (~55 lines) with simple OTEL setup
- **Recommendation:** Move shared telemetry to SharedKernel, service-specific to each service
- **How it was implemented:**
  Core: `ObservabilityConventions` (ActivitySourceName/MeterName = `"APITemplate"`), `ApiMetrics` (static `Meter` + counters for rate-limit rejections, handled exceptions). Domain-specific: `AuthTelemetry`, `CacheTelemetry`, `ConflictTelemetry`, `GraphQlTelemetry`, `ValidationTelemetry`, `StoredProcedureTelemetry`, `StartupTelemetry` — each with static methods wrapping `ActivitySource.StartActivity()`. Support: `HttpRouteResolver` (display-friendly route names), `TelemetryApiSurfaceResolver` (REST vs GraphQL classification), `HealthCheckMetricsPublisher` (health check results as metrics). Registration in `AddObservability()`: OpenTelemetry tracing (ASP.NET Core, HttpClient, HotChocolate, Redis, Npgsql, MongoDB) + metrics (runtime, process, custom histograms). Config section: `"Observability"` with `ObservabilityOptions` (ServiceName, Exporters.Aspire/Otlp/Console). Auto-detected Aspire in dev, OTLP in containers.

### 10. Inbound Webhook Processing
- Webhooks service only has outbound delivery
- **Recommendation:** Add inbound processing to Webhooks service
- **How it was implemented:**
  Full chain:
  - **Validator**: `HmacWebhookPayloadValidator` (`IWebhookPayloadValidator`) — HMAC-SHA256 signature + timestamp header validation.
  - **Filter**: `WebhookSignatureResourceFilter` (`IAsyncResourceFilter`) — read raw body with `EnableBuffering()`, checked for `[ValidateWebhookSignature]` attribute, threw `UnauthorizedException` on failure.
  - **Queue**: `ChannelWebhookQueue` (Channel-based `IWebhookProcessingQueue`) + `WebhookProcessingBackgroundService` consumer.
  - **Handler**: `LoggingWebhookEventHandler` (`IWebhookEventHandler`).
  - Config: `WebhookOptions` section. Headers: `WebhookConstants.SignatureHeader`, `WebhookConstants.TimestampHeader`. Controller: `[AllowAnonymous]`, 1MB request limit.

### 11. SSE Streaming Endpoint
- Not in any microservice
- **Recommendation:** Add to Notifications service or a dedicated Realtime service
- **How it was implemented:**
  `SseController` with `[HttpGet("stream")]`. Set headers: `text/event-stream`, `no-cache`, `keep-alive`. Sent `GetNotificationStreamQuery` via Wolverine `IMessageBus.InvokeAsync<IAsyncEnumerable<SseNotificationItem>>()`. Wrote `data: <json>\n\n` frames via `StreamWriter` on `Response.Body`, flushing after each item. Took `SseStreamRequest` from query params. Required `Permission.Examples.Read`.

### 12. JSON Patch Endpoint
- Not in any microservice
- **Recommendation:** Add to ProductCatalog if needed
- **How it was implemented:**
  `PatchController` with `[HttpPatch("products/{id:guid}")]`. Used `SystemTextJsonPatch` library (not Newtonsoft). Received `JsonPatchDocument<PatchableProductDto>`. Sent `PatchProductCommand(id, dto => patchDocument.ApplyTo(dto))` — command carried an `Action<PatchableProductDto>` apply-delegate so the application layer controlled mutation. Returned `ErrorOr<ProductResponse>` via `ToActionResult()`. Required `Permission.Examples.Update`.

### 13. Idempotent Endpoint
- SharedKernel has idempotency filter infrastructure, but no service endpoint uses it
- **Recommendation:** Apply `[Idempotent]` attribute where needed

### 14. Email Retry Background Job
- Not in BackgroundJobs microservice
- **Recommendation:** Add to BackgroundJobs service
- **How it was implemented:**
  TickerQ recurring job: `EmailRetryRecurringJob` with `[TickerFunction(TickerQFunctionNames.EmailRetry)]`. Gated by `IDistributedJobCoordinator.ExecuteIfLeaderAsync()` for multi-node safety. Delegated to `EmailRetryService` (`IEmailRetryService`). Optimistic per-record claiming: `ClaimRetryableBatchAsync()` with owner = `"{MachineName}:{ProcessId}"` and lease timeout. Per-email commit for crash safety. Dead-lettered via `ClaimExpiredBatchAsync()`. Config: `BackgroundJobsOptions.EmailRetry` with `EmailRetryJobOptions` (Cron=`"*/15 * * * *"`, MaxRetryAttempts=5, BatchSize=50, DeadLetterAfterHours=48, ClaimLeaseMinutes=10). Used Polly `SmtpSend` pipeline for delivery retry. Stored procedures: `ClaimExpiredFailedEmailsProcedure`, `ClaimRetryableFailedEmailsProcedure`.

### 15. Auth Bootstrap Seeder
- No microservice performs auth bootstrapping
- **Recommendation:** Add to Identity.Api startup
- **How it was implemented:**
  `AuthBootstrapSeeder` with deps: `AppDbContext`, `IOptions<BootstrapTenantOptions>`. Seeded default tenant (hardcoded ID `00000000-0000-0000-0000-000000000001`). Config: `Bootstrap:Tenant` section with `Code` and `Name`. Used `IgnoreQueryFilters(["SoftDelete", "Tenant"])` to find existing. Restored soft-deleted/deactivated tenants. Only called `SaveChangesAsync` if changes made. Called during startup in `UseDatabaseAsync()` wrapped by `StartupTelemetry.RunAuthBootstrapSeedAsync()`.

### 16. Startup Task Coordinator
- Prevents concurrent migrations in multi-instance deployments
- **Recommendation:** Add to SharedKernel.Infrastructure
- **How it was implemented:**
  `PostgresAdvisoryLockStartupTaskCoordinator` (`IStartupTaskCoordinator`). Used PostgreSQL `pg_advisory_lock(@lockKey)` / `pg_advisory_unlock(@lockKey)` on a dedicated `NpgsqlConnection`. `StartupTaskName` enum values as stable lock keys. Returned `IAsyncDisposable` lease (`PostgresAdvisoryLockLease`). Fell back to `NoOpAsyncDisposable` for non-Npgsql providers. Connection string from `DbContext.Database.GetConnectionString()`.

## Lower Impact - Completeness

### 17. SoftDeleteCascadeRule Implementations
- Interfaces exist in SharedKernel but no implementations in microservices
- **Recommendation:** Implement per-service as needed

### 18. Entity Normalization Service
- Interface in SharedKernel, no implementation in Identity
- **Recommendation:** Add to Identity.Infrastructure
- **How it was implemented:**
  `AppUserEntityNormalizationService` (`IEntityNormalizationService`). Single method `Normalize(IAuditableTenantEntity entity)` — type-checked for `AppUser`, then set `NormalizedUsername = AppUser.NormalizeUsername(user.Username)` and `NormalizedEmail = AppUser.NormalizeEmail(user.Email)`. Normalization methods were static on the `AppUser` domain entity.

### 19. MongoDB Migration Support
- ProductCatalog has MongoDbContext but no migration runner
- **Recommendation:** Add to ProductCatalog.Api startup
- **How it was implemented:**
  Used `Kot.MongoDB.Migrations` package. Called `IMigrator.MigrateAsync()` at startup in `UseDatabaseAsync()`.

### 20. MongoDB Health Check
- **Recommendation:** Add to ProductCatalog.Api
- **How it was implemented:**
  Custom `MongoDbHealthCheck` registered via `services.AddHealthChecks().AddCheck<MongoDbHealthCheck>()`.

### 21. Log Redaction / PII Classification
- **Recommendation:** Add to SharedKernel when compliance required
- **How it was implemented:**
  `Microsoft.Extensions.Compliance.Redaction` with `AddApplicationRedaction()` in `Program.cs`. `LogDataClassifications` defined taxonomies (Sensitive, Personal). Configured HMAC redaction for sensitive data, erasing for personal data.

### 22. External Integration Sync Job (preview)
- `IExternalIntegrationSyncService` with recurring job
- **Recommendation:** Add when needed

### 23. BackgroundJobs Options Validator
- **Recommendation:** Add to BackgroundJobs service
- **How it was implemented:**
  `BackgroundJobsOptionsValidator` — startup validation of job config using `IValidateOptions<T>`.

## Testing Gaps

### Missing test categories in microservices:
- **Auth integration tests** - no service tests auth flows end-to-end
- **Postgres-specific tests** - search, tenant isolation, cascade, transactions
- **GraphQL tests** - follows from feature gap
- **SSE/Patch/Idempotent/Webhook-receive tests** - follow from feature gaps
- **Infrastructure tests** - CORS, rate limiting behavior

### Testing infrastructure unique to monolith:
- Alba-based integration fixtures (microservices use WebApplicationFactory)
- InMemoryProductRepository for unit tests
- WebhookTestHelper
- TestOutputCacheStore
