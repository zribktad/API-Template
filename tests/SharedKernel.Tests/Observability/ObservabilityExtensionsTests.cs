using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SharedKernel.Api.Extensions;
using SharedKernel.Application.Options;
using SharedKernel.Infrastructure.Observability;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.Observability;

public sealed class ObservabilityExtensionsTests
{
    [Fact]
    public void GetEnabledOtlpEndpoints_WhenDevelopmentOutsideContainer_DefaultsToAspire()
    {
        ObservabilityOptions options = new();

        IReadOnlyList<string> endpoints = ObservabilityExtensions.GetEnabledOtlpEndpoints(
            options,
            new FakeHostEnvironment(Environments.Development)
        );

        endpoints.ShouldContain(TelemetryDefaults.AspireOtlpEndpoint);
    }

    [Fact]
    public void GetEnabledOtlpEndpoints_WhenExplicitOtlpEnabled_IncludesConfiguredEndpoint()
    {
        ObservabilityOptions options = new()
        {
            Otlp = new OtlpEndpointOptions { Endpoint = "http://alloy:4317" },
            Exporters = new ObservabilityExportersOptions
            {
                Otlp = new ObservabilityExporterToggleOptions { Enabled = true },
                Aspire = new ObservabilityExporterToggleOptions { Enabled = false },
            },
        };

        IReadOnlyList<string> endpoints = ObservabilityExtensions.GetEnabledOtlpEndpoints(
            options,
            new FakeHostEnvironment(Environments.Production)
        );

        endpoints.ShouldBe(["http://alloy:4317"]);
    }

    [Fact]
    public void BuildResourceAttributes_ReturnsExpectedMetadata()
    {
        Dictionary<string, object> attributes = ObservabilityExtensions.BuildResourceAttributes(
            "identity",
            new FakeHostEnvironment(Environments.Development)
        );

        attributes[TelemetryResourceAttributeKeys.ServiceName].ShouldBe("identity");
        attributes.ShouldContainKey(TelemetryResourceAttributeKeys.ServiceVersion);
        attributes.ShouldContainKey(TelemetryResourceAttributeKeys.ServiceInstanceId);
        attributes.ShouldContainKey(TelemetryResourceAttributeKeys.HostName);
        attributes.ShouldContainKey(TelemetryResourceAttributeKeys.HostArchitecture);
        attributes.ShouldContainKey(TelemetryResourceAttributeKeys.OsType);
        attributes.ShouldContainKey(TelemetryResourceAttributeKeys.ProcessRuntimeVersion);
    }

    [Fact]
    public void GetObservabilityOptions_WhenMissingConfiguration_ReturnsDefaults()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        ObservabilityOptions options = ObservabilityExtensions.GetObservabilityOptions(
            configuration
        );

        options.ShouldNotBeNull();
        options.Otlp.ShouldNotBeNull();
        options.Exporters.ShouldNotBeNull();
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "SharedKernel.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
