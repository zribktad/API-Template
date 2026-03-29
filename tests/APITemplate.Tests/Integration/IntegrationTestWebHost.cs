using APITemplate.Infrastructure.Persistence;
using APITemplate.Tests.Integration.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace APITemplate.Tests.Integration;

internal static class IntegrationTestWebHost
{
    internal static void Configure(IWebHostBuilder builder, string inMemoryDatabaseName)
    {
        builder.ConfigureAppConfiguration(
            (_, configBuilder) =>
            {
                var config = TestConfigurationHelper.GetBaseConfiguration();
                configBuilder.AddInMemoryCollection(config);
            }
        );

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));

            var optionsConfigs = services
                .Where(d =>
                    d.ServiceType.IsGenericType
                    && d.ServiceType.GetGenericTypeDefinition()
                        .FullName?.Contains("IDbContextOptionsConfiguration") == true
                )
                .ToList();

            foreach (var d in optionsConfigs)
                services.Remove(d);

            services.AddDbContext<AppDbContext>(options =>
                options
                    .UseInMemoryDatabase(inMemoryDatabaseName)
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            );

            TestServiceHelper.MockMongoServices(services);
            TestServiceHelper.RemoveExternalHealthChecks(services);
            TestServiceHelper.ReplaceProductRepositoryWithInMemory(services);
            TestServiceHelper.ReplaceOutputCacheWithInMemory(services);
            TestServiceHelper.ReplaceDataProtectionWithInMemory(services);
            TestServiceHelper.ReplaceTicketStoreWithInMemory(services);
            TestServiceHelper.ConfigureTestAuthentication(services);
            TestServiceHelper.RemoveTickerQRuntimeServices(services);
            TestServiceHelper.ReplaceStartupCoordinationWithNoOp(services);
        });

        builder.UseEnvironment("Development");
    }
}
