using Gateway.Api.Extensions;
using Scalar.AspNetCore;
using SharedKernel.Api.Extensions;
using SharedKernel.Application.Security;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSharedSerilog();
builder.Services.AddSharedObservability(builder.Configuration, builder.Environment, "gateway");
builder.Services.AddGatewayCors(builder.Configuration);
builder.Services.AddGatewayRateLimiting(builder.Configuration);

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

await app.WaitForKeycloakAsync();

app.UseSharedRequestLogging();
app.UseGatewayCors();
app.UseRateLimiter();

app.MapReverseProxy();
app.MapHealthChecks("/health");
app.MapGatewayScalarUi();

await app.RunAsync();

public static class GatewayDocumentationExtensions
{
    public static WebApplication MapGatewayScalarUi(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        IConfigurationSection keycloak = app.Configuration.GetRequiredSection("Keycloak");
        string authority = KeycloakAuthExtensions.BuildAuthority(
            keycloak["auth-server-url"]
                ?? throw new InvalidOperationException(
                    "Configuration key 'Keycloak:auth-server-url' is required."
                ),
            keycloak["realm"]
                ?? throw new InvalidOperationException(
                    "Configuration key 'Keycloak:realm' is required."
                )
        );

        app.MapScalarApiReference(
                "/scalar",
                (options, httpContext) =>
                {
                    string redirectUri = BuildScalarRedirectUri(httpContext.Request);

                    options.WithTitle("Gateway");
                    options
                        .AddDocument("identity", "Identity API", "/openapi/identity.json")
                        .AddDocument(
                            "product-catalog",
                            "Product Catalog API",
                            "/openapi/product-catalog.json"
                        )
                        .AddDocument("reviews", "Reviews API", "/openapi/reviews.json")
                        .AddDocument(
                            "file-storage",
                            "File Storage API",
                            "/openapi/file-storage.json"
                        )
                        .AddDocument(
                            "background-jobs",
                            "Background Jobs API",
                            "/openapi/background-jobs.json"
                        )
                        .AddDocument(
                            "notifications",
                            "Notifications API",
                            "/openapi/notifications.json"
                        )
                        .AddDocument("webhooks", "Webhooks API", "/openapi/webhooks.json")
                        .AddPreferredSecuritySchemes(SharedAuthConstants.OpenApi.OAuth2Scheme)
                        .AddAuthorizationCodeFlow(
                            SharedAuthConstants.OpenApi.OAuth2Scheme,
                            flow =>
                            {
                                flow.ClientId = SharedAuthConstants.OpenApi.ScalarClientId;
                                flow.SelectedScopes = [.. SharedAuthConstants.Scopes.Default];
                                flow.AuthorizationUrl =
                                    $"{authority}/{SharedAuthConstants.OpenIdConnect.AuthorizationEndpointPath}";
                                flow.TokenUrl =
                                    $"{authority}/{SharedAuthConstants.OpenIdConnect.TokenEndpointPath}";
                                flow.RedirectUri = redirectUri;
                                flow.Pkce = Pkce.Sha256;
                            }
                        );
                }
            )
            .AllowAnonymous();

        return app;
    }

    private static string BuildScalarRedirectUri(HttpRequest request) =>
        $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}";
}

public partial class Program;
