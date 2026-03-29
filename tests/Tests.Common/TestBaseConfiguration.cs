using System.Security.Cryptography;
using System.Text;

namespace TestCommon;

public static class TestBaseConfiguration
{
    public static Dictionary<string, string?> GetSharedConfiguration(string hmacKeySeed)
    {
        string testRedactionHmacKey = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(hmacKeySeed))
        );

        return new Dictionary<string, string?>
        {
            ["Keycloak:realm"] = "api-template",
            ["Keycloak:auth-server-url"] = "http://localhost:8180/",
            ["Keycloak:resource"] = "api-template",
            ["Keycloak:credentials:secret"] = "test-secret",
            ["Keycloak:SkipReadinessCheck"] = "true",
            ["SystemIdentity:DefaultActorId"] = "00000000-0000-0000-0000-000000000000",
            ["Bootstrap:Tenant:Code"] = "default",
            ["Bootstrap:Tenant:Name"] = "Default Tenant",
            ["Persistence:Transactions:IsolationLevel"] = "ReadCommitted",
            ["Persistence:Transactions:TimeoutSeconds"] = "30",
            ["Persistence:Transactions:RetryEnabled"] = "true",
            ["Persistence:Transactions:RetryCount"] = "3",
            ["Persistence:Transactions:RetryDelaySeconds"] = "5",
            ["Redaction:HmacKeyEnvironmentVariable"] = "APITEMPLATE_REDACTION_HMAC_KEY",
            ["Redaction:HmacKey"] = testRedactionHmacKey,
            ["Redaction:KeyId"] = "1001",
            ["Observability:Exporters:Aspire:Enabled"] = "false",
            ["Observability:Exporters:Otlp:Enabled"] = "false",
            ["Observability:Exporters:Console:Enabled"] = "false",
        };
    }
}
