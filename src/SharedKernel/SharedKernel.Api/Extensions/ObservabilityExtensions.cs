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
        string otlpEndpoint = configuration["Observability:Otlp:Endpoint"] ?? "http://alloy:4317";

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

                tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter("Wolverine");

                metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
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

        bool aspireEnabled = options.Exporters.Aspire.Enabled ?? environment.IsDevelopment();
        if (aspireEnabled)
        {
            endpoints.Add(
                string.IsNullOrWhiteSpace(options.Aspire.Endpoint)
                    ? TelemetryDefaults.AspireOtlpEndpoint
                    : options.Aspire.Endpoint
            );
        }

        bool otlpEnabled = options.Exporters.Otlp.Enabled ?? !environment.IsDevelopment();
        if (otlpEnabled && !string.IsNullOrWhiteSpace(options.Otlp.Endpoint))
        {
            endpoints.Add(options.Otlp.Endpoint);
        }

        return endpoints.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

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
