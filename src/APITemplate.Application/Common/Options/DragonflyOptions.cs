using System.ComponentModel.DataAnnotations;

namespace APITemplate.Application.Common.Options;

public sealed class DragonflyOptions
{
    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    public int ConnectTimeoutMs { get; init; } = 5000;

    public int SyncTimeoutMs { get; init; } = 3000;
}
