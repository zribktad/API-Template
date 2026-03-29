using TestCommon;

namespace Integration.Tests.Helpers;

internal static class CrossServiceConfigHelper
{
    internal static Dictionary<string, string?> GetBaseConfiguration(
        string serviceName,
        string connectionStringKey,
        string dbConnectionString,
        string rabbitMqConnectionString
    )
    {
        Dictionary<string, string?> config = TestBaseConfiguration.GetSharedConfiguration(
            $"Integration.Tests.{serviceName}"
        );

        config[$"ConnectionStrings:{connectionStringKey}"] = dbConnectionString;
        config["ConnectionStrings:RabbitMQ"] = rabbitMqConnectionString;
        config["ConnectionStrings__RabbitMQ"] = rabbitMqConnectionString;
        Uri rabbitUri = new(rabbitMqConnectionString);
        config["RabbitMQ:HostName"] = $"{rabbitUri.Host}:{rabbitUri.Port}";
        config[$"Observability:ServiceName"] = $"Integration.Tests.{serviceName}";
        config["Invitation:BaseUrl"] = "http://localhost:3000";
        config["Invitation:ExpirationHours"] = "72";
        config["Bff:CookieDomain"] = "localhost";
        config["Bff:CookieName"] = ".test";
        config["FileStorage:BasePath"] = Path.GetTempPath();

        return config;
    }
}
