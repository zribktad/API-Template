using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;
using SharedKernel.Api.Extensions;
using SharedKernel.Application.Security;

namespace SharedKernel.Api.OpenApi;

/// <summary>
/// Adds a Keycloak OAuth2 authorization code security scheme to OpenAPI documents.
/// </summary>
public sealed class BearerSecuritySchemeDocumentTransformer : IOpenApiDocumentTransformer
{
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IConfiguration _configuration;

    public BearerSecuritySchemeDocumentTransformer(
        IAuthenticationSchemeProvider schemeProvider,
        IConfiguration configuration
    )
    {
        _schemeProvider = schemeProvider;
        _configuration = configuration;
    }

    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        IEnumerable<AuthenticationScheme> schemes = await _schemeProvider.GetAllSchemesAsync();
        if (!schemes.Any(s => s.Name == JwtBearerDefaults.AuthenticationScheme))
        {
            return;
        }

        IConfigurationSection keycloak = _configuration.GetSection("Keycloak");
        string authServerUrl = keycloak["auth-server-url"] ?? string.Empty;
        string realm = keycloak["realm"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(authServerUrl) || string.IsNullOrWhiteSpace(realm))
        {
            return;
        }

        string authority = KeycloakAuthExtensions.BuildAuthority(authServerUrl, realm);
        var securityScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Keycloak OAuth2 Authorization Code flow",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri(
                        $"{authority}/{SharedAuthConstants.OpenIdConnect.AuthorizationEndpointPath}"
                    ),
                    TokenUrl = new Uri(
                        $"{authority}/{SharedAuthConstants.OpenIdConnect.TokenEndpointPath}"
                    ),
                    Scopes = new Dictionary<string, string>
                    {
                        [SharedAuthConstants.Scopes.OpenId] = "OpenID Connect",
                        [SharedAuthConstants.Scopes.Profile] = "User profile",
                        [SharedAuthConstants.Scopes.Email] = "Email address",
                    },
                },
            },
        };

        var components = document.Components ??= new OpenApiComponents();
        components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        components.SecuritySchemes[SharedAuthConstants.OpenApi.OAuth2Scheme] = securityScheme;

        var requirement = new OpenApiSecurityRequirement();
        requirement[
            new OpenApiSecuritySchemeReference(
                SharedAuthConstants.OpenApi.OAuth2Scheme,
                document,
                null
            )
        ] = [SharedAuthConstants.Scopes.OpenId];

        document.Security ??= [];
        document.Security.Add(requirement);
    }
}
