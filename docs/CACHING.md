# Caching

## Overview

The application uses **ASP.NET Core Output Cache** with an optional **Valkey** (Redis-compatible, BSD-3 licensed) backing store to cache HTTP GET responses.

- **With Valkey configured** — all application instances share the same cache, ensuring consistency behind a load balancer.
- **Without Valkey** — falls back to in-memory cache with a warning log. Suitable for local development and single-instance deployments.

## Architecture

```
Client Request
       │
       ▼
  Authentication
       │
       ▼
  Authorization
       │
       ▼
  Rate Limiting
       │
       ▼
  Output Cache Middleware ──── Valkey (shared store)
       │                         ▲
       ▼                         │
  Controller ────────────────────┘
  (EvictByTagAsync on mutations)
```

The Output Cache middleware runs **after** authentication and authorization, so unauthenticated or unauthorized requests are rejected before reaching the cache layer.

## Configuration

### Valkey Connection (Optional)

Configured via `appsettings.json` or environment variables:

```json
{
  "Valkey": {
    "ConnectionString": "localhost:6379"
  }
}
```

Environment variable override: `Valkey__ConnectionString`

When the `Valkey:ConnectionString` setting is **missing or empty**, the application logs a warning and uses the built-in in-memory output cache. No Valkey instance is required for development.

### Cache Policies

Defined in `ApiServiceCollectionExtensions.cs`:

| Policy | Expiration | Tag | Used By |
|---|---|---|---|
| *(base)* | No cache | — | All endpoints by default |
| `Products` | 30 seconds | `Products` | `ProductsController` GET endpoints |
| `Categories` | 60 seconds | `Categories` | `CategoriesController` GET endpoints |
| `Reviews` | 30 seconds | `Reviews` | `ProductReviewsController` GET endpoints |

The base policy disables caching for all endpoints. Only endpoints explicitly decorated with `[OutputCache(PolicyName = "...")]` are cached.

### Instance Name

All Valkey keys are prefixed with `ApiTemplate:OutputCache:` to avoid collisions with other applications sharing the same Valkey instance.

## How It Works

### Caching a Response

1. A GET request arrives and passes through authentication, authorization, and rate limiting.
2. The Output Cache middleware checks Valkey for a cached response matching the request.
3. **Cache hit** — the cached response is returned immediately; the controller is not invoked.
4. **Cache miss** — the request continues to the controller, the response is generated, stored in Valkey with the configured expiration and tag, and returned to the client.

### Cache Invalidation

Controllers invalidate cache after mutations (Create, Update, Delete) using tag-based eviction:

```csharp
await _outputCacheStore.EvictByTagAsync("Categories", ct);
```

This removes **all** cached responses tagged with the specified tag. For example, creating a new category evicts both the category list and all individual category responses.

#### Invalidation Map

| Action | Tags Evicted |
|---|---|
| Create/Update/Delete Product | `Products` |
| Delete Product | `Products`, `Reviews` |
| Create/Update/Delete Category | `Categories` |
| Create/Delete Review | `Reviews` |

Deleting a product also evicts reviews because product reviews become orphaned.

### Tags

Each policy assigns a tag to its cached responses. Tags group related cache entries so they can be invalidated together. A single `EvictByTagAsync` call removes all entries with that tag across all endpoints and URL variations.

## Infrastructure

### Docker Compose

Valkey is included in both `docker-compose.yml` (development) and `docker-compose.production.yml`:

```yaml
valkey:
  image: valkey/valkey:8-alpine
  ports:
    - "6379:6379"
  volumes:
    - valkeydata:/data
  healthcheck:
    test: ["CMD", "valkey-cli", "ping"]
    interval: 10s
    timeout: 5s
    retries: 5
```

### Health Check

Valkey health is monitored at `/health` alongside PostgreSQL, MongoDB, and Keycloak. The health check is tagged as `cache`.

## Adding a New Cache Policy

1. Add the policy in `ApiServiceCollectionExtensions.cs`:

```csharp
options.AddPolicy("MyEntity", builder => builder
    .Expire(TimeSpan.FromSeconds(30))
    .Tag("MyEntity"));
```

2. Decorate GET endpoints with the policy:

```csharp
[HttpGet]
[OutputCache(PolicyName = "MyEntity")]
public async Task<ActionResult<...>> GetAll(CancellationToken ct) { ... }
```

3. Invalidate after mutations:

```csharp
await _outputCacheStore.EvictByTagAsync("MyEntity", ct);
```

## Valkey vs Redis

Valkey is a community-maintained fork of Redis under the **BSD-3** license (fully open source), maintained by the Linux Foundation. It is wire-compatible with Redis — the same protocol, commands, and client libraries (`StackExchange.Redis`) work with both. The `AddStackExchangeRedisOutputCache` method works identically with Valkey.
