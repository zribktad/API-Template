using System.ComponentModel.DataAnnotations;

namespace FileStorage.Application.Common.Options;

/// <summary>
/// Configuration for the local file-storage provider, including the base directory, upload size limit,
/// and allowed file extensions.
/// </summary>
public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    [Required]
    public string BasePath { get; init; } = Path.Combine(Path.GetTempPath(), "filestorage-files");

    [Range(1, long.MaxValue)]
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024; // 10 MB

    [MinLength(1)]
    public string[] AllowedExtensions { get; init; } =
    [".jpg", ".png", ".gif", ".pdf", ".csv", ".txt"];
}
