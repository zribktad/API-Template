using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using SharedKernel.Messaging.Conventions;

namespace SharedKernel.Messaging.HealthChecks;

/// <summary>
/// Verifies that the configured RabbitMQ broker is reachable and accepts AMQP connections.
/// </summary>
public sealed class RabbitMqHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        string connectionString;
        try
        {
            connectionString = RabbitMqConventionExtensions.ResolveConnectionString(configuration);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ configuration is invalid.", ex);
        }

        try
        {
            ConnectionFactory factory = new() { Uri = new Uri(connectionString) };
            await using IConnection connection = await factory.CreateConnectionAsync();

            return connection.IsOpen
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("RabbitMQ connection was created but is not open.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ broker is unavailable.", ex);
        }
    }
}
