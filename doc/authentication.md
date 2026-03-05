# How Authentication Works (Authentik OIDC + Tenant Claims)

This guide explains the current authentication flow: Authentik as the Identity Provider, OIDC token validation, tenant claim enforcement, and endpoint protection.

---

## Overview

Authentication is delegated to **Authentik** (open-source Identity Provider). The API acts as an OIDC resource server that validates RS256-signed JWTs issued by Authentik. The login endpoint proxies credentials to Authentik via the Resource Owner Password Credentials (ROPC) grant.

```
Client  ->  POST /api/v1/Auth/login  ->  AuthController
                                          |
                                          v
                                   AuthentikAuthenticationProxy
                                          |  (ROPC grant: username, password)
                                          v
                                   Authentik Token Endpoint
                                          |
                                          v
                                   { accessToken, expiresAt }
                                          |
Client  ->  Authorization: Bearer <token>  ->  Protected REST/GraphQL endpoint
                                                  |
                                                  v
                                           JwtBearer middleware validates
                                           RS256 signature via OIDC discovery
                                           + enforces tenant_id claim
```

Startup (`UseDatabaseAsync`) runs `AuthBootstrapSeeder`, which ensures a default tenant exists in the application database. User management is handled entirely within Authentik.

---

## Step 1 - Configure Authentik Settings

`Authentik` section in appsettings controls OIDC connection:

```json
{
  "Authentik": {
    "Authority": "https://authentik.example.com/application/o/api-template/",
    "ClientId": "api-template",
    "ClientSecret": "<from-authentik-provider>",
    "TokenEndpoint": "https://authentik.example.com/application/o/token/",
    "TenantClaimType": "tenant_id",
    "RoleClaimType": "groups"
  }
}
```

`Bootstrap` controls the initial seeded tenant:

```json
{
  "Bootstrap": {
    "Tenant": {
      "Code": "default",
      "Name": "Default Tenant"
    }
  }
}
```

### Development

`appsettings.Development.json` points to the local Authentik instance running via Docker Compose on port 9000.

### Production

All `Authentik:*` values must be supplied securely (env vars/secret manager). The app validates options with `ValidateOnStart()` and fails fast if required values are missing.

---

## Step 2 - Obtain a Token

```http
POST /api/v1/Auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin-password"
}
```

The API proxies this to Authentik's token endpoint using the ROPC grant with configured `ClientId` and `ClientSecret`.

Success:

```json
{
  "accessToken": "<jwt>",
  "expiresAt": "2026-03-04T12:00:00Z"
}
```

Failure:
- `401 Unauthorized` with `LoginErrorResponse` when credentials are invalid or Authentik is unreachable.

---

## Step 3 - Use the Token

Use `Authorization: Bearer <token>` for protected endpoints:

```http
GET /api/v1/Products
Authorization: Bearer <jwt>
```

REST controllers are protected with `[Authorize]` (except `AuthController.Login`).
GraphQL query and mutation fields are protected with `[Authorize]`.

---

## Token Claims

Authentik issues these claims (configured via provider + property mapping):
- `sub` -> user id (Authentik user UUID)
- `tenant_id` -> tenant id (custom property mapping on Authentik scope `api`)
- `groups` -> user groups/roles (`PlatformAdmin`, `TenantUser`)
- `preferred_username` -> username
- `jti` -> token id

`JwtBearerOptions.OnTokenValidated` rejects tokens missing a valid `tenant_id` claim.

---

## Reading Claims in Code

```csharp
var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
var tenantId = User.FindFirstValue(CustomClaimTypes.TenantId);
// Role is read from "groups" claim (mapped to .NET roles via RoleClaimType)
```

---

## Protecting a New Endpoint

REST:

```csharp
[ApiController]
[Authorize]
public sealed class OrdersController : ControllerBase { }
```

GraphQL mutation:

```csharp
[Authorize]
public class OrderMutations { }
```

---

## Authentik Setup (Manual)

After `docker-compose up`, configure Authentik at `http://localhost:9000`:

1. Create an OAuth2/OIDC Provider: `api-template`, Client Type: Confidential, Signing Key: RS256
2. Create an Application linked to the provider
3. Create a Property Mapping (scope `api`):
   ```python
   return {"tenant_id": request.user.attributes.get("tenant_id", "")}
   ```
4. Create Authentik groups: `PlatformAdmin`, `TenantUser`
5. Create a user with custom attribute `tenant_id: "<guid>"` matching `Tenant.Id` in the app DB
6. Enable ROPC grant on the provider (for the login proxy)

---

## Key Files Reference

| File | Purpose |
|------|---------|
| `Api/Controllers/V1/AuthController.cs` | Login endpoint (proxies to Authentik) |
| `Application/Features/Auth/Interfaces/IAuthenticationProxy.cs` | Authentication proxy interface |
| `Infrastructure/Security/AuthentikAuthenticationProxy.cs` | ROPC grant implementation |
| `Application/Common/Options/AuthentikOptions.cs` | Strongly-typed Authentik options |
| `Application/Common/Security/CustomClaimTypes.cs` | `tenant_id` claim type constant |
| `Application/Common/Options/BootstrapTenantOptions.cs` | Seeded tenant options |
| `Infrastructure/Persistence/AuthBootstrapSeeder.cs` | Startup bootstrap seed for tenant |
| `Extensions/ServiceCollectionExtensions.cs` | `AddAuthenticationOptions()` + `AddAuthentikAuthentication()` |
| `Extensions/ApplicationBuilderExtensions.cs` | `UseDatabaseAsync()` runs seeding |
| `appsettings.json` | Base config (`Authentik`, `Bootstrap`, `SystemIdentity`, `Cors`) |
| `appsettings.Development.json` | Development overrides (local Authentik URL) |
