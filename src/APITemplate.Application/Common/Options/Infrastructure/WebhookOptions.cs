using System.ComponentModel.DataAnnotations;

namespace APITemplate.Application.Common.Options.Infrastructure;

public sealed class WebhookOptions
{
    [Required]
    [MinLength(16, ErrorMessage = "Webhook secret must be at least 16 characters.")]
    public string Secret { get; set; } = string.Empty;

    public int TimestampToleranceSeconds { get; set; } = 300; // 5 minutes
}
