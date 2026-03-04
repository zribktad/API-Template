using System.ComponentModel.DataAnnotations;
using APITemplate.Domain.Entities;

namespace APITemplate.Application.Common.Options;

public sealed class SystemIdentityOptions
{
    [Required]
    public string DefaultActorId { get; init; } = AuditDefaults.SystemActorId;
}
