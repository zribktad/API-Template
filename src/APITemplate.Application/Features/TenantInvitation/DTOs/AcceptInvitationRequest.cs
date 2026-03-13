using APITemplate.Application.Common.Validation;

namespace APITemplate.Application.Features.TenantInvitation.DTOs;

public sealed record AcceptInvitationRequest([NotEmpty] string Token);
