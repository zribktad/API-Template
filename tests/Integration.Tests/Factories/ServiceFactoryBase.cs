using System.Security.Cryptography;
using Integration.Tests.Fixtures;
using Integration.Tests.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Xunit;

namespace Integration.Tests.Factories;

public abstract class ServiceFactoryBase<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime
    where TProgram : class
{
    private static readonly RSA RsaKey = RSA.Create(2048);
    private static readonly RsaSecurityKey SecurityKey = new(RsaKey);
    private const string TestIssuer = "http://localhost:8180/realms/api-template";
    private const string TestAudience = "api-template";

    private readonly SharedContainers _containers;
    private readonly string _databaseName = $"test_{Guid.NewGuid():N}";

    protected ServiceFactoryBase(SharedContainers containers)
    {
        _containers = containers;
    }

    protected abstract string ServiceName { get; }
    protected abstract string ConnectionStringKey { get; }

    public string ConnectionString =>
        new NpgsqlConnectionStringBuilder(_containers.PostgresServerConnectionString)
        {
            Database = _databaseName,
        }.ConnectionString;

    public async ValueTask InitializeAsync()
    {
        await using NpgsqlConnection conn = new(_containers.PostgresServerConnectionString);
        await conn.OpenAsync();
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
        await cmd.ExecuteNonQueryAsync();

        // Pre-warm: trigger host build so EF migrations run before tests execute.
        _ = Services;
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        try
        {
            await using NpgsqlConnection conn = new(_containers.PostgresServerConnectionString);
            await conn.OpenAsync();
            await using NpgsqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{_databaseName}' AND pid <> pg_backend_pid();
                DROP DATABASE IF EXISTS "{_databaseName}";
                """;
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best-effort cleanup — container disposal handles the rest.
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Dictionary<string, string?> config = CrossServiceConfigHelper.GetBaseConfiguration(
            ServiceName,
            ConnectionStringKey,
            ConnectionString,
            _containers.RabbitMqConnectionString
        );

        ConfigureAdditionalConfiguration(config);

        builder.ConfigureAppConfiguration(
            (_, configBuilder) => configBuilder.AddInMemoryCollection(config)
        );

        builder.ConfigureTestServices(services =>
        {
            ConfigureTestAuthentication(services);
            RemoveExternalHealthChecks(services);
            ConfigureServiceSpecificMocks(services);
        });

        builder.UseEnvironment("Development");
    }

    protected virtual void ConfigureAdditionalConfiguration(Dictionary<string, string?> config) { }

    protected virtual void ConfigureServiceSpecificMocks(IServiceCollection services) { }

    private static void ConfigureTestAuthentication(IServiceCollection services)
    {
        services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                options.Authority = null;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = TestIssuer,
                    ValidateAudience = true,
                    ValidAudience = TestAudience,
                    ValidateLifetime = true,
                    IssuerSigningKey = SecurityKey,
                    ValidateIssuerSigningKey = true,
                };
            }
        );
    }

    private static void RemoveExternalHealthChecks(IServiceCollection services)
    {
        services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(
            options =>
            {
                List<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration> toRemove =
                    options
                        .Registrations.Where(r =>
                            r.Name.Contains("mongodb", StringComparison.OrdinalIgnoreCase)
                            || r.Name.Contains("keycloak", StringComparison.OrdinalIgnoreCase)
                            || r.Name.Contains("dragonfly", StringComparison.OrdinalIgnoreCase)
                        )
                        .ToList();

                foreach (
                    Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration r in toRemove
                )
                {
                    options.Registrations.Remove(r);
                }
            }
        );
    }
}
