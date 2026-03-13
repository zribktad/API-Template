using APITemplate.Application.Common.Validation;
using APITemplate.Application.Features.TenantInvitation.DTOs;

namespace APITemplate.Application.Features.TenantInvitation.Validation;

public sealed class CreateTenantInvitationRequestValidator
    : DataAnnotationsValidator<CreateTenantInvitationRequest>;
