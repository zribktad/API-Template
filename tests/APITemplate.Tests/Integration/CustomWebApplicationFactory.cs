using APITemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace APITemplate.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(
                TestServiceCollectionExtensions.GetTestConfiguration());
        });

        builder.ConfigureTestServices(services =>
        {
            services.ConfigureTestAuthentication();
            services.RemoveDbContextRegistrations();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName)
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            services.MockMongoDb();
        });

        builder.UseEnvironment("Development");
    }
}
