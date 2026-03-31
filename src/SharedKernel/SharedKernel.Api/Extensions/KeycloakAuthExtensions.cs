using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Application.Security;

namespace SharedKernel.Api.Extensions;

public static class KeycloakAuthExtensions
{
    public static AuthenticationBuilder AddSharedKeycloakJwtBearer(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        bool requireTenantClaim = true,
        Action<JwtBearerOptions>? configureOptions = null
    )
    {
        AuthenticationBuilder authBuilder = services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                IConfigurationSection keycloak = configuration.GetRequiredSection("Keycloak");
                string authServerUrl = GetRequiredValue(keycloak, "auth-server-url");
                string realm = GetRequiredValue(keycloak, "realm");
                string resource = GetRequiredValue(keycloak, "resource");

                options.Authority = BuildAuthority(authServerUrl, realm);
                options.Audience = resource;
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.TokenValidationParameters = BuildTokenValidationParameters(
                    environment.IsDevelopment()
                );

                configureOptions?.Invoke(options);
                WrapTokenValidated(options, requireTenantClaim);
            });

        return authBuilder;
    }

    public static string BuildAuthority(string authServerUrl, string realm) =>
        $"{authServerUrl.TrimEnd('/')}/realms/{realm}";

    private static TokenValidationParameters BuildTokenValidationParameters(bool isDevelopment) =>
        new()
        {
            LogTokenId = isDevelopment,
            LogValidationExceptions = isDevelopment,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            RequireAudience = true,
            SaveSigninToken = false,
            TryAllIssuerSigningKeys = true,
            ValidateActor = false,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidateTokenReplay = false,
            ClockSkew = TimeSpan.FromMinutes(5),
        };

    private static void WrapTokenValidated(JwtBearerOptions options, bool requireTenantClaim)
    {
        JwtBearerEvents existingEvents = options.Events ?? new JwtBearerEvents();
        Func<TokenValidatedContext, Task>? existingHandler = existingEvents.OnTokenValidated;

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                KeycloakClaimsPrincipalMapper.MapClaims(context.Principal);

                if (existingHandler is not null)
                    await existingHandler(context);

                if (requireTenantClaim && !HasValidTenantClaim(context.Principal))
                    context.Fail($"Missing required {SharedAuthConstants.Claims.TenantId} claim.");
            },
            OnAuthenticationFailed = existingEvents.OnAuthenticationFailed,
            OnChallenge = existingEvents.OnChallenge,
            OnForbidden = existingEvents.OnForbidden,
            OnMessageReceived = existingEvents.OnMessageReceived,
        };
    }

    private static bool HasValidTenantClaim(ClaimsPrincipal? principal) =>
        principal?.HasClaim(c =>
            c.Type == SharedAuthConstants.Claims.TenantId
            && Guid.TryParse(c.Value, out Guid tenantId)
            && tenantId != Guid.Empty
        ) == true
        || IsServiceAccount(principal);

    private static bool IsServiceAccount(ClaimsPrincipal? principal)
    {
        string? username = principal?.FindFirstValue(
            SharedAuthConstants.KeycloakClaims.PreferredUsername
        );
        return username != null
            && username.StartsWith(
                SharedAuthConstants.KeycloakClaims.ServiceAccountUsernamePrefix,
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static string GetRequiredValue(IConfigurationSection section, string key) =>
        !string.IsNullOrWhiteSpace(section[key])
            ? section[key]!
            : throw new InvalidOperationException(
                $"Configuration key '{section.Path}:{key}' is required."
            );
}
