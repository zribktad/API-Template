namespace APITemplate.Application.Common.Options.Security;

public sealed class CorsOptions
{
    public string[] AllowedOrigins { get; init; } = [];
}
