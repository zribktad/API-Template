using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SharedKernel.Infrastructure.Observability;

/// <summary>Shared names for the application's OpenTelemetry activity source and meters.</summary>
public static class ObservabilityConventions
{
    public const string ActivitySourceName = "APITemplate";
    public const string MeterName = "APITemplate";
    public const string HealthMeterName = "APITemplate.Health";

    /// <summary>Shared activity source — use this instead of creating new instances per class.</summary>
    internal static readonly ActivitySource SharedActivitySource = new(ActivitySourceName);

    /// <summary>Shared meter for application metrics — use this instead of creating new instances per class.</summary>
    internal static readonly Meter SharedMeter = new(MeterName);

    /// <summary>Shared meter for health check metrics.</summary>
    internal static readonly Meter SharedHealthMeter = new(HealthMeterName);
}

/// <summary>Canonical metric instrument names emitted by the application meter.</summary>
public static class TelemetryMetricNames
{
    public const string AuthFailures = "apitemplate_auth_failures";
    public const string ConcurrencyConflicts = "apitemplate_concurrency_conflicts";
    public const string DomainConflicts = "apitemplate_domain_conflicts";
    public const string HandledExceptions = "apitemplate_exceptions_handled";
    public const string HealthStatus = "apitemplate_healthcheck_status";
    public const string OutputCacheInvalidations = "apitemplate_output_cache_invalidations";
    public const string OutputCacheInvalidationDuration =
        "apitemplate_output_cache_invalidation_duration";
    public const string OutputCacheOutcomes = "apitemplate_output_cache_outcomes";
    public const string ValidationErrors = "apitemplate_validation_errors";
    public const string ValidationRequestsRejected = "apitemplate_validation_requests_rejected";
}

/// <summary>Canonical tag/attribute key names applied to metrics and traces.</summary>
public static class TelemetryTagKeys
{
    public const string ApiSurface = "apitemplate.api.surface";
    public const string Authenticated = "apitemplate.authenticated";
    public const string AuthFailureReason = "auth.failure_reason";
    public const string AuthScheme = "auth.scheme";
    public const string CacheOutcome = "cache.outcome";
    public const string CachePolicy = "cache.policy";
    public const string CacheTag = "cache.tag";
    public const string DbSystem = "db.system";
    public const string ErrorCode = "error.code";
    public const string ExceptionType = "exception.type";
    public const string HttpRoute = "http.route";
    public const string Service = "service";
    public const string StartupComponent = "startup.component";
    public const string StartupStep = "startup.step";
    public const string StartupSuccess = "startup.success";
    public const string TenantId = "tenant.id";
    public const string ValidationDtoType = "validation.dto_type";
    public const string ValidationProperty = "validation.property";
}

/// <summary>Canonical activity/span names recorded in the application activity source.</summary>
public static class TelemetryActivityNames
{
    public const string OutputCacheInvalidate = "output_cache.invalidate";
    public const string TokenValidated = "auth.token-validated";

    public static string Startup(string step) => $"startup.{step}";
}

/// <summary>Well-known output-cache outcome values.</summary>
public static class TelemetryOutcomeValues
{
    public const string Bypass = "bypass";
    public const string Hit = "hit";
    public const string Store = "store";
}

/// <summary>Well-known authentication failure reasons.</summary>
public static class TelemetryFailureReasons
{
    public const string AuthenticationFailed = "authentication_failed";
    public const string MissingTenantClaim = "missing_tenant_claim";
}

/// <summary>Well-known tag values that identify the API surface a request was served from.</summary>
public static class TelemetrySurfaces
{
    public const string Documentation = "documentation";
    public const string GraphQl = "graphql";
    public const string Health = "health";
    public const string Rest = "rest";
}

/// <summary>Default fallback values used when a tag or setting cannot be resolved.</summary>
public static class TelemetryDefaults
{
    public const string AspireOtlpEndpoint = "http://localhost:4317";
    public const string Default = "default";
    public const string Unknown = "unknown";
}

/// <summary>Keys used to store transient telemetry values in <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/>.</summary>
public static class TelemetryContextKeys
{
    public const string OutputCachePolicyName = "OutputCachePolicyName";
}

/// <summary>URL path prefixes used to classify requests into API surface areas.</summary>
public static class TelemetryPathPrefixes
{
    public const string GraphQl = "/graphql";
    public const string Health = "/health";
    public const string OpenApi = "/openapi";
    public const string Scalar = "/scalar";
}

/// <summary>Well-known step names used to identify individual startup task activities.</summary>
public static class TelemetryStartupSteps
{
    public const string Migrate = "migrate";
}

/// <summary>Well-known component names tagged on startup activity spans.</summary>
public static class TelemetryStartupComponents
{
    public const string PostgreSql = "postgresql";
}

/// <summary>Well-known database system tag values.</summary>
public static class TelemetryDatabaseSystems
{
    public const string PostgreSql = "postgresql";
}

/// <summary>Meter names from ASP.NET Core and Microsoft libraries used to subscribe to built-in metrics.</summary>
public static class TelemetryMeterNames
{
    public const string AspNetCoreAuthentication = "Microsoft.AspNetCore.Authentication";
    public const string AspNetCoreAuthorization = "Microsoft.AspNetCore.Authorization";
    public const string AspNetCoreConnections = "Microsoft.AspNetCore.Http.Connections";
    public const string AspNetCoreDiagnostics = "Microsoft.AspNetCore.Diagnostics";
    public const string AspNetCoreHosting = "Microsoft.AspNetCore.Hosting";
    public const string AspNetCoreRateLimiting = "Microsoft.AspNetCore.RateLimiting";
    public const string AspNetCoreRouting = "Microsoft.AspNetCore.Routing";
    public const string AspNetCoreServerKestrel = "Microsoft.AspNetCore.Server.Kestrel";
}

/// <summary>Semantic-convention instrument names for HTTP client and server request durations.</summary>
public static class TelemetryInstrumentNames
{
    public const string HttpClientRequestDuration = "http.client.request.duration";
    public const string HttpServerRequestDuration = "http.server.request.duration";
}

/// <summary>OpenTelemetry resource attribute key names.</summary>
public static class TelemetryResourceAttributeKeys
{
    public const string AssemblyName = "assembly.name";
    public const string DeploymentEnvironmentName = "deployment.environment.name";
    public const string HostArchitecture = "host.arch";
    public const string HostName = "host.name";
    public const string OsType = "os.type";
    public const string ProcessPid = "process.pid";
    public const string ProcessRuntimeName = "process.runtime.name";
    public const string ProcessRuntimeVersion = "process.runtime.version";
    public const string ServiceInstanceId = "service.instance.id";
    public const string ServiceName = "service.name";
    public const string ServiceNamespace = "service.namespace";
    public const string ServiceVersion = "service.version";
}

/// <summary>Pre-defined histogram bucket boundaries for common metric instruments.</summary>
public static class TelemetryHistogramBoundaries
{
    public static readonly double[] HttpRequestDurationSeconds =
    [
        0.005,
        0.01,
        0.025,
        0.05,
        0.075,
        0.1,
        0.25,
        0.5,
        0.75,
        1,
        2.5,
        5,
        10,
    ];

    public static readonly double[] CacheOperationDurationMs =
    [
        1,
        5,
        10,
        25,
        50,
        100,
        250,
        500,
        1000,
    ];
}

/// <summary>Third-party library names used as OpenTelemetry activity sources and meters.</summary>
public static class TelemetryThirdPartySources
{
    public const string Wolverine = "Wolverine";
}
