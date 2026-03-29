using Microsoft.Extensions.Configuration;
using SharedKernel.Messaging.Topology;
using Wolverine;
using Wolverine.RabbitMQ;

namespace SharedKernel.Messaging.Conventions;

/// <summary>
/// Standardised RabbitMQ transport registration for all microservices.
/// Reads the connection from <c>ConnectionStrings:RabbitMQ</c> (full URI) with a fallback
/// to the legacy <c>RabbitMQ:HostName</c> setting.
/// </summary>
public static class RabbitMqConventionExtensions
{
    private const string DefaultRabbitMqUri = "amqp://guest:guest@localhost:5672";

    /// <summary>
    /// Configures the Wolverine RabbitMQ transport using a shared connection strategy.
    /// </summary>
    public static WolverineOptions UseSharedRabbitMq(
        this WolverineOptions opts,
        IConfiguration configuration
    )
    {
        string connectionString =
            configuration.GetConnectionString("RabbitMQ") ?? BuildFromHostName(configuration);

        opts.UseRabbitMq(new Uri(connectionString))
            .AutoProvision()
            .EnableWolverineControlQueues()
            .ConfigureListeners(listener =>
            {
                listener.UseInterop(new TenantAwareEnvelopeMapper());
            });

        return opts;
    }

    private static string BuildFromHostName(IConfiguration configuration)
    {
        string? hostName = configuration["RabbitMQ:HostName"];
        return hostName is not null ? $"amqp://{hostName}" : DefaultRabbitMqUri;
    }
}
