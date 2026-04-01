namespace SharedKernel.Application.Options;

/// <summary>
/// Root configuration object for observability exporters and endpoints.
/// </summary>
public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public OtlpEndpointOptions Otlp { get; init; } = new();

    public AspireEndpointOptions Aspire { get; init; } = new();

    public ObservabilityExportersOptions Exporters { get; init; } = new();
}

public sealed class OtlpEndpointOptions
{
    public string Endpoint { get; init; } = string.Empty;
}

public sealed class AspireEndpointOptions
{
    public string Endpoint { get; init; } = string.Empty;
}

public sealed class ObservabilityExportersOptions
{
    public ObservabilityExporterToggleOptions Aspire { get; init; } = new();

    public ObservabilityExporterToggleOptions Otlp { get; init; } = new();

    public ObservabilityExporterToggleOptions Console { get; init; } = new();
}

public sealed class ObservabilityExporterToggleOptions
{
    public bool? Enabled { get; init; }
}
