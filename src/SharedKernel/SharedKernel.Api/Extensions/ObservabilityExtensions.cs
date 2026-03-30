using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Npgsql;
using OpenTelemetry.Instrumentation.AspNetCore;
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
        services.AddValidatedOptions<ObservabilityOptions>(
            configuration,
            ObservabilityOptions.SectionName
        );
        services.AddSingleton<IHealthCheckPublisher, HealthCheckMetricsPublisher>();
        services.Configure<HealthCheckPublisherOptions>(options =>
        {
            options.Delay = TimeSpan.FromSeconds(15);
            options.Period = TimeSpan.FromMinutes(5);
        });

        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;

        ObservabilityOptions options = GetObservabilityOptions(configuration);
        Dictionary<string, object> resourceAttributes = BuildResourceAttributes(
            serviceName,
            environment
        );
        bool enableConsoleExporter = IsConsoleExporterEnabled(options);
        IReadOnlyList<string> otlpEndpoints = GetEnabledOtlpEndpoints(options, environment);

        var openTelemetryBuilder = services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddAttributes(resourceAttributes));

        openTelemetryBuilder.WithTracing(builder =>
        {
            builder
                .AddAspNetCoreInstrumentation(ConfigureAspNetCoreTracing)
                .AddHttpClientInstrumentation()
                .AddRedisInstrumentation()
                .AddNpgsql()
                .AddSource(
                    ObservabilityConventions.ActivitySourceName,
                    TelemetryThirdPartySources.Wolverine
                );

            ConfigureTracingExporters(builder, otlpEndpoints, enableConsoleExporter);
        });

        openTelemetryBuilder.WithMetrics(builder =>
        {
            builder
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddMeter(
                    ObservabilityConventions.MeterName,
                    ObservabilityConventions.HealthMeterName,
                    TelemetryMeterNames.AspNetCoreHosting,
                    TelemetryMeterNames.AspNetCoreServerKestrel,
                    TelemetryMeterNames.AspNetCoreConnections,
                    TelemetryMeterNames.AspNetCoreRouting,
                    TelemetryMeterNames.AspNetCoreDiagnostics,
                    TelemetryMeterNames.AspNetCoreRateLimiting,
                    TelemetryMeterNames.AspNetCoreAuthentication,
                    TelemetryMeterNames.AspNetCoreAuthorization,
                    TelemetryThirdPartySources.Wolverine
                )
                .AddView(
                    TelemetryInstrumentNames.HttpServerRequestDuration,
                    new ExplicitBucketHistogramConfiguration
                    {
                        Boundaries = TelemetryHistogramBoundaries.HttpRequestDurationSeconds,
                    }
                )
                .AddView(
                    TelemetryInstrumentNames.HttpClientRequestDuration,
                    new ExplicitBucketHistogramConfiguration
                    {
                        Boundaries = TelemetryHistogramBoundaries.HttpRequestDurationSeconds,
                    }
                )
                .AddView(
                    TelemetryMetricNames.OutputCacheInvalidationDuration,
                    new ExplicitBucketHistogramConfiguration
                    {
                        Boundaries = TelemetryHistogramBoundaries.CacheOperationDurationMs,
                    }
                );

            ConfigureMetricExporters(builder, otlpEndpoints, enableConsoleExporter);
        });

        return services;
    }

    internal static IReadOnlyList<string> GetEnabledOtlpEndpoints(
        ObservabilityOptions options,
        IHostEnvironment environment
    )
    {
        List<string> endpoints = [];

        if (IsAspireExporterEnabled(options, environment))
        {
            string aspireEndpoint = string.IsNullOrWhiteSpace(options.Aspire.Endpoint)
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

    internal static bool IsAspireExporterEnabled(
        ObservabilityOptions options,
        IHostEnvironment environment
    ) =>
        options.Exporters.Aspire.Enabled
        ?? (environment.IsDevelopment() && !IsRunningInContainer());

    internal static bool IsOtlpExporterEnabled(
        ObservabilityOptions options,
        IHostEnvironment environment
    ) => options.Exporters.Otlp.Enabled ?? IsRunningInContainer();

    internal static bool IsConsoleExporterEnabled(ObservabilityOptions options) =>
        options.Exporters.Console.Enabled ?? false;

    internal static ObservabilityOptions GetObservabilityOptions(IConfiguration configuration) =>
        configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>()
        ?? new();

    internal static Dictionary<string, object> BuildResourceAttributes(
        string serviceName,
        IHostEnvironment environment
    )
    {
        AssemblyName? entryAssembly = Assembly.GetEntryAssembly()?.GetName();
        string machineName = Environment.MachineName;
        int processId = Environment.ProcessId;

        return new Dictionary<string, object>
        {
            [TelemetryResourceAttributeKeys.AssemblyName] = entryAssembly?.Name ?? serviceName,
            [TelemetryResourceAttributeKeys.ServiceName] = serviceName,
            [TelemetryResourceAttributeKeys.ServiceNamespace] = serviceName,
            [TelemetryResourceAttributeKeys.ServiceVersion] =
                entryAssembly?.Version?.ToString() ?? TelemetryDefaults.Unknown,
            [TelemetryResourceAttributeKeys.ServiceInstanceId] = $"{machineName}-{processId}",
            [TelemetryResourceAttributeKeys.DeploymentEnvironmentName] =
                environment.EnvironmentName,
            [TelemetryResourceAttributeKeys.HostName] = machineName,
            [TelemetryResourceAttributeKeys.HostArchitecture] =
                RuntimeInformation.OSArchitecture.ToString(),
            [TelemetryResourceAttributeKeys.OsType] = GetOsType(),
            [TelemetryResourceAttributeKeys.ProcessPid] = processId,
            [TelemetryResourceAttributeKeys.ProcessRuntimeName] = ".NET",
            [TelemetryResourceAttributeKeys.ProcessRuntimeVersion] = Environment.Version.ToString(),
        };
    }

    internal static void ConfigureAspNetCoreTracing(AspNetCoreTraceInstrumentationOptions options)
    {
        options.RecordException = true;
        options.Filter = httpContext =>
            !httpContext.Request.Path.StartsWithSegments(TelemetryPathPrefixes.Health);
        options.EnrichWithHttpRequest = (activity, httpRequest) =>
        {
            if (TelemetryApiSurfaceResolver.Resolve(httpRequest.Path) != TelemetrySurfaces.Rest)
                return;

            string route = HttpRouteResolver.Resolve(httpRequest.HttpContext);
            activity.DisplayName = $"{httpRequest.Method} {route}";
            activity.SetTag(TelemetryTagKeys.HttpRoute, route);
        };
    }

    private static bool IsRunningInContainer() =>
        string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase
        );

    private static string GetOsType() =>
        OperatingSystem.IsWindows() ? "windows"
        : OperatingSystem.IsLinux() ? "linux"
        : OperatingSystem.IsMacOS() ? "darwin"
        : TelemetryDefaults.Unknown;

    private static void ConfigureTracingExporters(
        TracerProviderBuilder builder,
        IReadOnlyList<string> otlpEndpoints,
        bool enableConsoleExporter
    )
    {
        foreach (string endpoint in otlpEndpoints)
        {
            builder.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
        }

        if (enableConsoleExporter)
            builder.AddConsoleExporter();
    }

    private static void ConfigureMetricExporters(
        MeterProviderBuilder builder,
        IReadOnlyList<string> otlpEndpoints,
        bool enableConsoleExporter
    )
    {
        foreach (string endpoint in otlpEndpoints)
        {
            builder.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
        }

        if (enableConsoleExporter)
            builder.AddConsoleExporter();
    }
}
