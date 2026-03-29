using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProductCatalog.Infrastructure.Persistence;

namespace ProductCatalog.Infrastructure.HealthChecks;

public sealed class MongoDbHealthCheck : IHealthCheck
{
    private readonly MongoDbContext _mongoDbContext;

    public MongoDbHealthCheck(MongoDbContext mongoDbContext)
    {
        _mongoDbContext = mongoDbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await _mongoDbContext.PingAsync(cancellationToken);
            return HealthCheckResult.Healthy("MongoDB connection is healthy.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB connection failed.", ex);
        }
    }
}
