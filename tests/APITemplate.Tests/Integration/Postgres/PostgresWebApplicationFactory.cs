using APITemplate.Application.Common.Security;
using APITemplate.Domain.Interfaces;
using APITemplate.Infrastructure.Persistence;
using APITemplate.Infrastructure.Security;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Testcontainers.PostgreSql;
using Xunit;

namespace APITemplate.Tests.Integration.Postgres;

public sealed class PostgresWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase($"apitemplate_tests_{Guid.NewGuid():N}")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithCleanUp(true)
        .Build();

    public Task InitializeAsync() => _postgresContainer.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var testRedactionHmacKey = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes("APITemplate.Tests.RedactionKey.Postgres")));

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgresContainer.GetConnectionString(),
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
                ["Redaction:HmacKey"] = testRedactionHmacKey,
                ["Redaction:KeyId"] = "1001"
            });
        });

        builder.ConfigureTestServices(services =>
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

            // Remove eagerly-captured Npgsql registrations so the container connection string is used.
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));

            var optionsConfigs = services
                .Where(d =>
                    d.ServiceType.IsGenericType &&
                    d.ServiceType.GetGenericTypeDefinition().FullName?
                        .Contains("IDbContextOptionsConfiguration") == true)
                .ToList();

            foreach (var d in optionsConfigs)
                services.Remove(d);

            // Re-register with the test container's connection string.
            var connectionString = _postgresContainer.GetConnectionString();
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Replace the health check that was registered with the default connection string.
            var healthCheckDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("HealthCheck") == true)
                .ToList();

            foreach (var d in healthCheckDescriptors)
                services.Remove(d);

            services.AddHealthChecks()
                .AddNpgSql(connectionString, name: "postgresql", tags: ["database"]);

            // MongoDB is intentionally disabled in integration tests.
            services.RemoveAll(typeof(MongoDbContext));
            services.RemoveAll(typeof(IProductDataRepository));
            services.AddSingleton(new Mock<IProductDataRepository>().Object);
        });

        builder.UseEnvironment("Development");
    }
}
