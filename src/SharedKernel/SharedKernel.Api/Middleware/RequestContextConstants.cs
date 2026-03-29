namespace SharedKernel.Api.Middleware;

public static class RequestContextConstants
{
    public static class Headers
    {
        public const string CorrelationId = "X-Correlation-Id";
        public const string TraceId = "X-Trace-Id";
        public const string ElapsedMs = "X-Elapsed-Ms";
    }

    public static class LogProperties
    {
        public const string CorrelationId = "CorrelationId";
        public const string TenantId = "TenantId";
    }

    public static class MetricTags
    {
        public const string ApiSurface = "api.surface";
        public const string Authenticated = "authenticated";
    }
}
