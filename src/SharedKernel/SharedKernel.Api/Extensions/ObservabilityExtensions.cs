using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SharedKernel.Api.Observability;

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
                ConfigureTracing(tracing, environment, otlpEndpoint);
            })
            .WithMetrics(metrics =>
            {
                ConfigureMetrics(metrics, otlpEndpoint);
            });

        services.AddSingleton<IHealthCheckPublisher, HealthCheckMetricsPublisher>();

        return services;
    }

    private static void ConfigureTracing(
        TracerProviderBuilder tracing,
        IHostEnvironment environment,
        string otlpEndpoint
    )
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddNpgsql()
            .AddSource("Wolverine")
            .AddSource(ObservabilityConventions.ActivitySourceName)
            .AddSource("StackExchange.Redis")
            .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources");

        if (environment.IsDevelopment())
            tracing.AddConsoleExporter();

        tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    }

    private static void ConfigureMetrics(MeterProviderBuilder metrics, string otlpEndpoint)
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddMeter("Wolverine")
            .AddMeter(ObservabilityConventions.MeterName);

        metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    }
}
