using Identity.Application.Options;
using Identity.Application.Security;
using Identity.Infrastructure.Security.Tenant;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SharedKernel.Api.Extensions;

namespace Identity.Api.Extensions;

public static class BffAuthenticationExtensions
{
    public static AuthenticationBuilder AddIdentityBffAuthentication(
        this AuthenticationBuilder authBuilder,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        KeycloakOptions keycloak =
            configuration.GetRequiredSection(KeycloakOptions.SectionName).Get<KeycloakOptions>()
            ?? throw new InvalidOperationException("Keycloak configuration is missing.");

        BffOptions bff =
            configuration.GetRequiredSection(BffOptions.SectionName).Get<BffOptions>()
            ?? throw new InvalidOperationException("Bff configuration is missing.");

        string authority = KeycloakAuthExtensions.BuildAuthority(
            keycloak.AuthServerUrl,
            keycloak.Realm
        );

        return authBuilder
            .AddCookie(
                AuthConstants.BffSchemes.Cookie,
                options =>
                {
                    options.Cookie.Name = bff.CookieName;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = environment.IsDevelopment()
                        ? CookieSecurePolicy.SameAsRequest
                        : CookieSecurePolicy.Always;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(bff.SessionTimeoutMinutes);
                    options.SlidingExpiration = true;
                }
            )
            .AddOpenIdConnect(
                AuthConstants.BffSchemes.Oidc,
                options =>
                {
                    options.Authority = authority;
                    options.RequireHttpsMetadata = !environment.IsDevelopment();
                    options.ClientId = keycloak.Resource;
                    options.ClientSecret = keycloak.Credentials.Secret;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.SaveTokens = true;
                    options.SignInScheme = AuthConstants.BffSchemes.Cookie;

                    options.Scope.Clear();
                    foreach (string scope in bff.Scopes)
                        options.Scope.Add(scope);

                    options.Events = new OpenIdConnectEvents
                    {
                        OnTokenValidated = TenantClaimValidator.OnTokenValidated,
                    };
                }
            );
    }
}
