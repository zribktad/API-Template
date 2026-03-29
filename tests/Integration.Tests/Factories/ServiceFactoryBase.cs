using Integration.Tests.Fixtures;
using Integration.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TestCommon;
using Xunit;

namespace Integration.Tests.Factories;

public abstract class ServiceFactoryBase<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime
    where TProgram : class
{
    private readonly SharedContainers _containers;
    private readonly string _databaseName = $"test_{Guid.NewGuid():N}";

    protected ServiceFactoryBase(SharedContainers containers)
    {
        _containers = containers;
    }

    protected abstract string ServiceName { get; }
    protected abstract string ConnectionStringKey { get; }

    public string ConnectionString =>
        TestDatabaseLifecycle.BuildConnectionString(
            _containers.PostgresServerConnectionString,
            _databaseName
        );

    public async ValueTask InitializeAsync()
    {
        await TestDatabaseLifecycle.CreateDatabaseAsync(
            _containers.PostgresServerConnectionString,
            _databaseName
        );
    }

    public new async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        catch (OperationCanceledException) { }
        catch (AggregateException ex)
            when (ex.InnerExceptions.All(e =>
                    e is OperationCanceledException or TaskCanceledException
                )
            ) { }

        await TestDatabaseLifecycle.DropDatabaseAsync(
            _containers.PostgresServerConnectionString,
            _databaseName
        );
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Dictionary<string, string?> config = CrossServiceConfigHelper.GetBaseConfiguration(
            ServiceName,
            ConnectionStringKey,
            ConnectionString,
            _containers.RabbitMqConnectionString
        );

        // Shared infrastructure requires this options section in every service host.
        config["TransactionDefaults:IsolationLevel"] = "ReadCommitted";
        config["TransactionDefaults:TimeoutSeconds"] = "30";
        config["TransactionDefaults:RetryEnabled"] = "true";
        config["TransactionDefaults:RetryCount"] = "3";
        config["TransactionDefaults:RetryDelaySeconds"] = "5";
        config["ConnectionStrings:Dragonfly"] = string.Empty;
        config["ConnectionStrings__Dragonfly"] = string.Empty;

        ConfigureAdditionalConfiguration(config);

        // Ensure values are available early for Program-time configuration reads.
        foreach ((string key, string? value) in config)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                builder.UseSetting(key, value);
            }
        }

        builder.ConfigureAppConfiguration(
            (_, configBuilder) => configBuilder.AddInMemoryCollection(config)
        );

        builder.ConfigureTestServices(services =>
        {
            TestAuthSetup.ConfigureTestJwtBearer(services);
            RemoveExternalHealthChecks(services);
            ConfigureServiceSpecificMocks(services);
        });

        builder.UseEnvironment("Development");
    }

    protected virtual void ConfigureAdditionalConfiguration(Dictionary<string, string?> config) { }

    protected virtual void ConfigureServiceSpecificMocks(IServiceCollection services) { }

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
