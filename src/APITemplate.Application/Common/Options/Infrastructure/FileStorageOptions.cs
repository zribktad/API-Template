namespace APITemplate.Application.Common.Options.Infrastructure;

public sealed class FileStorageOptions
{
    public string BasePath { get; set; } = Path.Combine(Path.GetTempPath(), "api-template-files");
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB
    public string[] AllowedExtensions { get; set; } =
    [".jpg", ".png", ".gif", ".pdf", ".csv", ".txt"];
}
