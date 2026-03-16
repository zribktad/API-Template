using System.ComponentModel.DataAnnotations;

namespace APITemplate.Application.Common.Options;

public sealed class DragonflyOptions
{
    public const int DefaultConnectTimeoutMs = 5000;
    public const int DefaultSyncTimeoutMs = 3000;

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    public int ConnectTimeoutMs { get; init; } = DefaultConnectTimeoutMs;

    public int SyncTimeoutMs { get; init; } = DefaultSyncTimeoutMs;
}
