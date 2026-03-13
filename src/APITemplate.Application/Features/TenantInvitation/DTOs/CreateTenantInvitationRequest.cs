using System.ComponentModel.DataAnnotations;
using APITemplate.Application.Common.Validation;

namespace APITemplate.Application.Features.TenantInvitation.DTOs;

public sealed record CreateTenantInvitationRequest(
    [NotEmpty] [MaxLength(320)] [EmailAddress] string Email
);
