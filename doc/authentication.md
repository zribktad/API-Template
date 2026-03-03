# How Authentication Works (JWT)

This guide explains the JWT authentication flow used in the project, how to obtain a token, how to use it, and how to extend the implementation for production use.

---

## Overview

The project uses **ASP.NET Core JWT Bearer authentication**. The flow is:

```
Client  →  POST /api/v1/Auth/login  →  AuthController
                                            ↓
                                       UserService.ValidateAsync()
                                            ↓
                                       TokenService.GenerateToken()
                                            ↓
                                       { accessToken, expiresAt }
                                            ↓
Client  →  Authorization: Bearer <token>  →  Any protected endpoint
```

All controllers except `AuthController` require a valid token (`[Authorize]` attribute on the class). The GraphQL mutation classes use `[HotChocolate.Authorization.Authorize]`.

---

## Step 1 – Configure JWT Settings

### Development

`appsettings.Development.json` ships with a placeholder secret that satisfies the startup validation so the app runs out-of-the-box in the `Development` environment:

```json
{
  "Jwt": {
    "Secret": "DevelopmentOnlySuperSecretKeyAtLeast32Chars!",
    "Issuer": "APITemplate",
    "Audience": "APITemplate.Clients",
    "ExpirationMinutes": 60
  },
  "Auth": {
    "Username": "admin",
    "Password": "admin"
  }
}
```

> ⚠️ This placeholder is intentionally **not suitable for production**. Replace it before deploying.

### Production

The base `appsettings.json` intentionally leaves `Jwt:Secret` empty. The app **will not start** unless a real secret (>= 32 characters) is supplied at runtime. Provide it through one of the following mechanisms:

**Environment variable (recommended):**
```bash
# ASP.NET Core flattens __ into : so this maps to Jwt:Secret
Jwt__Secret=<your-256-bit-or-longer-secret>
```

**Docker / Compose secret:**
```yaml
environment:
  - Jwt__Secret=${JWT_SECRET}
```

**Cloud secrets manager** (Azure Key Vault, AWS Secrets Manager, etc.) – inject the value as the `Jwt__Secret` environment variable or mount it via the provider's configuration source.

> ⚠️ **Never commit real secrets** to source control. The startup validation will throw an `OptionsValidationException` at startup if the secret is missing or shorter than 32 characters, giving you a clear fail-fast signal before the app accepts traffic.

---

## Step 2 – Obtain a Token

```http
POST /api/v1/Auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "changeme"
}
```

**Response (200 OK):**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-01-01T13:00:00Z"
}
```

**Error (401 Unauthorized):** credentials did not match.

---

## Step 3 – Use the Token

Include the token in the `Authorization` header for all subsequent requests:

```http
GET /api/v1/Products
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

Or in the [Scalar](https://scalar.com/) / Swagger UI: click **Authorize** → enter the token (without the `Bearer ` prefix).

---

## How the Token Is Generated

`TokenService` (`Application/Services/TokenService.cs`) creates a signed JWT using the strongly-typed `JwtOptions` (injected via `IOptions<JwtOptions>`):

```csharp
public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _jwt;

    public TokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwt = jwtOptions.Value;
    }

    public TokenResponse GenerateToken(string username)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwt.Secret));

        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             _jwt.Issuer,
            audience:           _jwt.Audience,
            claims:             claims,
            expires:            expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
```

---

## How Token Validation Is Configured

Options are registered and validated at startup in `ServiceCollectionExtensions.AddAuthenticationOptions()`. The startup validation uses `.ValidateOnStart()` so a misconfigured secret (missing or shorter than 32 characters) causes an `OptionsValidationException` before the app accepts any traffic:

```csharp
services.AddOptions<JwtOptions>()
    .Bind(configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .Validate(
        o => !string.IsNullOrWhiteSpace(o.Secret) && o.Secret.Length >= 32,
        "Jwt secret too short")
    .Validate(
        o => !string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience),
        "Jwt issuer/audience is required")
    .ValidateOnStart();
```

The `JwtBearerOptions` are then wired up from the validated `JwtOptions`:

```csharp
services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptionsAccessor) =>
    {
        var jwt = jwtOptionsAccessor.Value;
        var key = Encoding.UTF8.GetBytes(jwt.Secret);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwt.Issuer,
            ValidAudience            = jwt.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(key)
        };
    });
```

`app.UseAuthentication()` and `app.UseAuthorization()` are called in `Program.cs` in the correct order.

---

## Protecting a New Endpoint

Add `[Authorize]` to the controller class (protects all actions) or to individual actions:

```csharp
// Entire controller requires a valid token
[ApiController]
[Authorize]
public sealed class OrdersController : ControllerBase { ... }

// Or allow anonymous access to specific actions
[HttpGet("public-summary")]
[AllowAnonymous]
public IActionResult GetPublicSummary() { ... }
```

For GraphQL mutations, use the HotChocolate attribute:

```csharp
using HotChocolate.Authorization;

[Authorize]
public class OrderMutations { ... }
```

---

## Reading the Authenticated User in a Controller

```csharp
[HttpGet("me")]
public IActionResult GetCurrentUser()
{
    var username = User.FindFirstValue(ClaimTypes.Name);
    return Ok(new { username });
}
```

---

## Replacing the Demo User Store (Production)

`UserService` currently validates against credentials stored in configuration — suitable only for demos. Replace it with a real user store:

### Option A – Database Users (EF Core)

```csharp
public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<bool> ValidateAsync(string username, string password, CancellationToken ct)
    {
        var user = await _userRepository.GetByUsernameAsync(username, ct);
        if (user is null) return false;

        // Use a constant-time comparison library — never compare plain-text passwords.
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }
}
```

### Option B – External Identity Provider (OAuth 2.0 / OpenID Connect)

Replace the local token generation entirely with an identity server (e.g., Keycloak, Auth0, Microsoft Entra ID) and configure `AddJwtBearer` to validate tokens issued by the external provider:

```csharp
options.Authority  = "https://your-identity-provider.com";
options.Audience   = "your-api-audience";
```

---

## Key Files Reference

| File | Purpose |
|------|---------|
| `Api/Controllers/V1/AuthController.cs` | Login endpoint |
| `Application/Services/TokenService.cs` | JWT generation |
| `Application/Services/UserService.cs` | Credential validation (replace in production) |
| `Application/Options/JwtOptions.cs` | Strongly-typed JWT configuration class |
| `Application/Options/AuthOptions.cs` | Strongly-typed Auth configuration class |
| `Application/DTOs/LoginRequest.cs` | Login request DTO |
| `Application/DTOs/TokenResponse.cs` | Token response DTO |
| `Extensions/ServiceCollectionExtensions.cs` | `AddAuthenticationOptions()` / `AddJwtAuthentication()` |
| `Program.cs` | `app.UseAuthentication()` / `app.UseAuthorization()` |
| `appsettings.json` | Base config – `Jwt:Secret` intentionally empty (must be overridden in production) |
| `appsettings.Development.json` | Dev-only overrides – includes a placeholder secret for local development |
