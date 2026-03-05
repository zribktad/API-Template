using System.ComponentModel.DataAnnotations;

namespace APITemplate.Application.Common.Options;

public sealed class AuthentikOptions
{
    [Required]
    public string Authority { get; init; } = string.Empty;

    [Required]
    public string ClientId { get; init; } = string.Empty;

    [Required]
    public string ClientSecret { get; init; } = string.Empty;

    [Required]
    public string TokenEndpoint { get; init; } = string.Empty;

    public string TenantClaimType { get; init; } = "tenant_id";

    public string RoleClaimType { get; init; } = "groups";
}
