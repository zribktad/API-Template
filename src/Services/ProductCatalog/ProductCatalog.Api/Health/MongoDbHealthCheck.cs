using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProductCatalog.Infrastructure.Persistence;

namespace ProductCatalog.Api.Health;

/// <summary>
/// Verifies MongoDB availability using the application's configured MongoDbContext.
/// </summary>
public sealed class MongoDbHealthCheck(IMongoDbHealthProbe mongoDbHealthProbe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await mongoDbHealthProbe.PingAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB server is unavailable.", ex);
        }
    }
}
