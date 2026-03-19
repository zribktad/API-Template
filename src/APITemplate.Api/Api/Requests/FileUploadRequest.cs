using System.ComponentModel.DataAnnotations;

namespace APITemplate.Api.Requests;

public sealed class FileUploadRequest
{
    [Required]
    public IFormFile File { get; init; } = null!;

    public string? Description { get; init; }
}
