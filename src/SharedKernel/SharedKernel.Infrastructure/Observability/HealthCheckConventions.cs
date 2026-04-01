namespace SharedKernel.Infrastructure.Observability;

/// <summary>
/// Shared names and tags for infrastructure health checks exposed by microservice hosts.
/// </summary>
public static class HealthCheckNames
{
    public const string PostgreSql = "postgres";
    public const string MongoDb = "mongo";
    public const string Dragonfly = "dragonfly";
    public const string RabbitMq = "rabbitmq";
    public const string Scheduler = "scheduler";
}

/// <summary>
/// Common tags used to classify health checks by dependency type.
/// </summary>
public static class HealthCheckTags
{
    public static readonly string[] Database = ["database", "infrastructure"];
    public static readonly string[] Cache = ["cache", "infrastructure"];
    public static readonly string[] Messaging = ["messaging", "infrastructure"];
    public static readonly string[] Scheduler = ["scheduler", "infrastructure"];
}
