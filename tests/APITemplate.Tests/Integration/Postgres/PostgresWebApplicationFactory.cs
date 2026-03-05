using APITemplate.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        var connectionString = _postgresContainer.GetConnectionString();

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(
                TestServiceCollectionExtensions.GetTestConfiguration(
                    hmacKeySeed: "APITemplate.Tests.RedactionKey.Postgres",
                    connectionString: connectionString));
        });

        builder.ConfigureTestServices(services =>
        {
            services.ConfigureTestAuthentication();
            services.RemoveDbContextRegistrations();

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

            services.MockMongoDb();
        });

        builder.UseEnvironment("Development");
    }
}
