using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Infrastructure.Startup;

namespace SharedKernel.Api.Extensions;

public static class HostExtensions
{
    public static async Task MigrateDbAsync<TDbContext>(this WebApplication app)
        where TDbContext : DbContext
    {
        using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public static async Task MigrateDbWithLockAsync<TDbContext>(this WebApplication app)
        where TDbContext : DbContext
    {
        IStartupTaskCoordinator coordinator =
            app.Services.GetRequiredService<IStartupTaskCoordinator>();
        await using IAsyncDisposable lease = await coordinator.AcquireAsync(
            StartupTaskName.DatabaseMigration
        );

        using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
