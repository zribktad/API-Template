using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel.Api.Extensions;
using SharedKernel.Application.Options.Security;
using SharedKernel.Infrastructure.Logging;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.Logging;

public sealed class RedactionConfigurationTests
{
    [Fact]
    public void ResolveHmacKey_WhenEnvironmentVariablePresent_PrefersEnvironmentValue()
    {
        RedactionOptions options = new()
        {
            HmacKeyEnvironmentVariable = "TEST_REDACTION_KEY",
            HmacKey = "config-value",
        };

        string hmacKey = RedactionConfiguration.ResolveHmacKey(
            options,
            variable => variable == "TEST_REDACTION_KEY" ? "env-value" : null
        );

        hmacKey.ShouldBe("env-value");
    }

    [Fact]
    public void ResolveHmacKey_WhenEnvironmentVariableMissing_UsesInlineConfiguration()
    {
        RedactionOptions options = new()
        {
            HmacKeyEnvironmentVariable = "TEST_REDACTION_KEY",
            HmacKey = "config-value",
        };

        string hmacKey = RedactionConfiguration.ResolveHmacKey(options, _ => null);

        hmacKey.ShouldBe("config-value");
    }

    [Fact]
    public void ResolveHmacKey_WhenNoSourceConfigured_Throws()
    {
        RedactionOptions options = new() { HmacKeyEnvironmentVariable = "TEST_REDACTION_KEY" };

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            RedactionConfiguration.ResolveHmacKey(options, _ => null)
        );

        exception.Message.ShouldContain("TEST_REDACTION_KEY");
    }

    [Fact]
    public void AddSharedLogRedaction_WithInlineHmacKey_RegistersLogging()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Redaction:HmacKeyEnvironmentVariable"] = "UNUSED_REDACTION_KEY",
                    ["Redaction:HmacKey"] = "unit-test-hmac-key",
                    ["Redaction:KeyId"] = "1001",
                }
            )
            .Build();

        services.AddSharedLogRedaction(configuration);

        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(ILoggerFactory));
    }

    [Fact]
    public void AddSharedLogRedaction_WhenMissingKey_Throws()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Redaction:HmacKeyEnvironmentVariable"] = "NON_EXISTENT_TEST_REDACTION_KEY",
                    ["Redaction:KeyId"] = "1001",
                }
            )
            .Build();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            services.AddSharedLogRedaction(configuration)
        );

        exception.Message.ShouldContain("NON_EXISTENT_TEST_REDACTION_KEY");
    }
}
