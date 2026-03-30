using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SharedKernel.Infrastructure.Observability;
using SharedKernel.Messaging.HealthChecks;

namespace SharedKernel.Api.Extensions;

/// <summary>
/// Shared infrastructure health-check registration helpers for microservice hosts.
/// </summary>
public static class HealthChecksServiceCollectionExtensions
{
    public static IHealthChecksBuilder AddPostgreSqlHealthCheck(
        this IHealthChecksBuilder builder,
        string connectionString,
        string name = HealthCheckNames.PostgreSql,
        string[]? tags = null
    ) => builder.AddNpgSql(connectionString, name: name, tags: tags ?? HealthCheckTags.Database);

    public static IHealthChecksBuilder AddDragonflyHealthCheck(
        this IHealthChecksBuilder builder,
        string? connectionString,
        string name = HealthCheckNames.Dragonfly,
        string[]? tags = null
    )
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return builder;

        return builder.AddRedis(connectionString, name: name, tags: tags ?? HealthCheckTags.Cache);
    }

    public static IHealthChecksBuilder AddSharedRabbitMqHealthCheck(
        this IHealthChecksBuilder builder,
        IConfiguration configuration,
        string name = HealthCheckNames.RabbitMq,
        string[]? tags = null
    ) => builder.AddCheck<RabbitMqHealthCheck>(name, tags: tags ?? HealthCheckTags.Messaging);
}
