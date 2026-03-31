using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SharedKernel.Application.Options;
using SharedKernel.Infrastructure.Observability;

namespace SharedKernel.Api.Extensions;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddSharedObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string serviceName
    )
    {
        ObservabilityOptions observabilityOptions = GetObservabilityOptions(configuration);
        IReadOnlyList<string> otlpEndpoints = GetEnabledOtlpEndpoints(
            observabilityOptions,
            environment
        );

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService(serviceName: serviceName, serviceVersion: "1.0.0")
            )
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddNpgsql()
                    .AddSource("Wolverine");

                if (environment.IsDevelopment())
                    tracing.AddConsoleExporter();

                foreach (string endpoint in otlpEndpoints)
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(endpoint));
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter("Wolverine");

                foreach (string endpoint in otlpEndpoints)
                    metrics.AddOtlpExporter(o => o.Endpoint = new Uri(endpoint));
            });

        return services;
    }

    internal static ObservabilityOptions GetObservabilityOptions(IConfiguration configuration) =>
        configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>()
        ?? new ObservabilityOptions();

    internal static IReadOnlyList<string> GetEnabledOtlpEndpoints(
        ObservabilityOptions options,
        IHostEnvironment environment
    )
    {
        List<string> endpoints = [];

        if (IsAspireExporterEnabled(options, environment))
        {
            var aspireEndpoint = string.IsNullOrWhiteSpace(options.Aspire.Endpoint)
                ? TelemetryDefaults.AspireOtlpEndpoint
                : options.Aspire.Endpoint;
            endpoints.Add(aspireEndpoint);
        }

        if (
            IsOtlpExporterEnabled(options, environment)
            && !string.IsNullOrWhiteSpace(options.Otlp.Endpoint)
        )
        {
            endpoints.Add(options.Otlp.Endpoint);
        }

        return endpoints.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>
    /// Returns whether the Aspire OTLP exporter is active: uses the explicit configuration value
    /// when set, otherwise defaults to true in Development outside a container.
    /// </summary>
    internal static bool IsAspireExporterEnabled(
        ObservabilityOptions options,
        IHostEnvironment environment
    ) =>
        options.Exporters.Aspire.Enabled
        ?? (environment.IsDevelopment() && !IsRunningInContainer());

    /// <summary>
    /// Returns whether the generic OTLP exporter is active: uses the explicit configuration value
    /// when set, otherwise defaults to true when running in a container.
    /// </summary>
    internal static bool IsOtlpExporterEnabled(
        ObservabilityOptions options,
        IHostEnvironment environment
    ) => options.Exporters.Otlp.Enabled ?? IsRunningInContainer();

    /// <summary>Returns whether the console/stdout exporter is enabled; defaults to false.</summary>
    internal static bool IsConsoleExporterEnabled(ObservabilityOptions options) =>
        options.Exporters.Console.Enabled ?? false;

    private static bool IsRunningInContainer() =>
        string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase
        );

    internal static Dictionary<string, object> BuildResourceAttributes(
        string serviceName,
        IHostEnvironment environment
    ) =>
        new(StringComparer.Ordinal)
        {
            [TelemetryResourceAttributeKeys.ServiceName] = serviceName,
            [TelemetryResourceAttributeKeys.ServiceVersion] = "1.0.0",
            [TelemetryResourceAttributeKeys.ServiceInstanceId] = Environment.MachineName,
            [TelemetryResourceAttributeKeys.HostName] = Environment.MachineName,
            [TelemetryResourceAttributeKeys.HostArchitecture] =
                System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
            [TelemetryResourceAttributeKeys.OsType] = System
                .Runtime
                .InteropServices
                .RuntimeInformation
                .OSDescription,
            [TelemetryResourceAttributeKeys.ProcessRuntimeVersion] = Environment.Version.ToString(),
            [TelemetryResourceAttributeKeys.DeploymentEnvironmentName] =
                environment.EnvironmentName,
        };
}
