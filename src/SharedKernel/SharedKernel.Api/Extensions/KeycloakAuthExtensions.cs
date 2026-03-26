using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SharedKernel.Api.Extensions;

public static class KeycloakAuthExtensions
{
    public static AuthenticationBuilder AddSharedKeycloakJwtBearer(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
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

                configureOptions?.Invoke(options);
            });

        return authBuilder;
    }

    public static string BuildAuthority(string authServerUrl, string realm) =>
        $"{authServerUrl.TrimEnd('/')}/realms/{realm}";

    private static string GetRequiredValue(IConfigurationSection section, string key) =>
        !string.IsNullOrWhiteSpace(section[key])
            ? section[key]!
            : throw new InvalidOperationException(
                $"Configuration key '{section.Path}:{key}' is required."
            );
}
