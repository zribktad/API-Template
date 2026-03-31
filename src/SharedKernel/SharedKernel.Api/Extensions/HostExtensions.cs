using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
}
