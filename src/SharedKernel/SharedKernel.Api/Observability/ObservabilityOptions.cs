using System.ComponentModel.DataAnnotations;

namespace SharedKernel.Api.Observability;

/// <summary>
/// Configuration options for the observability pipeline, bound to the "Observability" section.
/// </summary>
public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    [Required]
    public string ServiceName { get; init; } = default!;

    public string OtlpEndpoint { get; init; } = "http://alloy:4317";

    public ExporterOptions Exporters { get; init; } = new();
}

public sealed class ExporterOptions
{
    public bool Console { get; init; }
    public bool Otlp { get; init; } = true;
}
