using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Infrastructure.Observability;

namespace SharedKernel.Api.Extensions;

public static class HostExtensions
{
    public static async Task MigrateDbAsync<TDbContext>(this WebApplication app)
        where TDbContext : DbContext
    {
        using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        using StartupTelemetry.Scope telemetry = StartupTelemetry.StartRelationalMigration();
        try
        {
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            telemetry.Fail(ex);
            throw;
        }
    }
}
