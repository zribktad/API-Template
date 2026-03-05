using APITemplate.Application.Common.Security;
using APITemplate.Domain.Interfaces;
using APITemplate.Infrastructure.Persistence;
using APITemplate.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.Security.Cryptography;
using System.Text;

namespace APITemplate.Tests.Integration;

internal static class TestServiceCollectionExtensions
{
    public static Dictionary<string, string?> GetTestConfiguration(
        string hmacKeySeed = "APITemplate.Tests.RedactionKey",
        string? connectionString = null)
    {
        var hmacKey = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(hmacKeySeed)));

        var config = new Dictionary<string, string?>
        {
            ["Authentik:Authority"] = "http://localhost:9000/application/o/api-template/",
            ["Authentik:ClientId"] = "api-template",
            ["Authentik:ClientSecret"] = "test-secret",
            ["Authentik:TokenEndpoint"] = "http://localhost:9000/application/o/token/",
            ["SystemIdentity:DefaultActorId"] = "system",
            ["Bootstrap:Tenant:Code"] = "default",
            ["Bootstrap:Tenant:Name"] = "Default Tenant",
            ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
            ["Bff:Authority"] = "http://localhost:9000/application/o/api-template-bff/",
            ["Bff:ClientId"] = "api-template-bff",
            ["Bff:ClientSecret"] = "test-bff-secret",
            ["Redaction:HmacKeyEnvironmentVariable"] = "APITEMPLATE_REDACTION_HMAC_KEY",
            ["Redaction:HmacKey"] = hmacKey,
            ["Redaction:KeyId"] = "1001"
        };

        if (connectionString is not null)
            config["ConnectionStrings:DefaultConnection"] = connectionString;

        return config;
    }

    public static void ConfigureTestAuthentication(this IServiceCollection services)
    {
        // Remove all existing JWT Bearer option configurations (Configure + PostConfigure)
        // so OIDC discovery is fully disabled and test RSA keys are used instead.
        services.RemoveAll<IConfigureOptions<JwtBearerOptions>>();
        services.RemoveAll<IPostConfigureOptions<JwtBearerOptions>>();

        services.Configure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = TestAuthKeys.Issuer,
                    ValidateAudience = true,
                    ValidAudience = TestAuthKeys.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = TestAuthKeys.RsaSecurityKey,
                    RoleClaimType = "groups"
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (!TenantClaimValidator.HasValidTenantClaim(context.Principal))
                            context.Fail("Missing required tenant_id claim.");

                        return Task.CompletedTask;
                    }
                };
            });

        // Disable HTTPS check and provide static OIDC configuration to skip discovery.
        services.Configure<OpenIdConnectOptions>(
            BffAuthenticationSchemes.Oidc,
            options =>
            {
                options.RequireHttpsMetadata = false;
                options.Configuration = new OpenIdConnectConfiguration
                {
                    Issuer = TestAuthKeys.Issuer,
                    AuthorizationEndpoint = "http://localhost:9000/authorize",
                    TokenEndpoint = "http://localhost:9000/token",
                    EndSessionEndpoint = "http://localhost:9000/logout"
                };
            });
    }

    public static void RemoveDbContextRegistrations(this IServiceCollection services)
    {
        services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
        services.RemoveAll(typeof(AppDbContext));

        // Remove IDbContextOptionsConfiguration<> — critical for .NET 9/10.
        var optionsConfigs = services
            .Where(d =>
                d.ServiceType.IsGenericType &&
                d.ServiceType.GetGenericTypeDefinition().FullName?
                    .Contains("IDbContextOptionsConfiguration") == true)
            .ToList();

        foreach (var d in optionsConfigs)
            services.Remove(d);
    }

    public static void MockMongoDb(this IServiceCollection services)
    {
        services.RemoveAll(typeof(MongoDbContext));
        services.RemoveAll(typeof(IProductDataRepository));
        services.AddSingleton(new Mock<IProductDataRepository>().Object);
    }
}
