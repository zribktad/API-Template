using TestCommon;

namespace APITemplate.Tests.Integration.Helpers;

internal static class TestConfigurationHelper
{
    internal const string TestWebhookSecret = "test-webhook-secret-at-least-16-chars";

    internal static Dictionary<string, string?> GetBaseConfiguration(
        string hmacKeySeed = "APITemplate.Tests.RedactionKey"
    )
    {
        Dictionary<string, string?> config = TestBaseConfiguration.GetSharedConfiguration(
            hmacKeySeed
        );

        config["ConnectionStrings:DefaultConnection"] =
            "Host=localhost;Database=apitemplate_tests;Username=postgres;Password=postgres";
        config["BackgroundJobs:TickerQ:Enabled"] = "false";
        config["Observability:ServiceName"] = "APITemplate.Tests";
        config["Cors:AllowedOrigins:0"] = "http://localhost:3000";
        config["Dragonfly:ConnectionString"] = "";
        config["Webhook:Secret"] = TestWebhookSecret;

        return config;
    }
}
