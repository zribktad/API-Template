using APITemplate.Application.Common.Validation;
using APITemplate.Application.Features.PasswordReset.DTOs;

namespace APITemplate.Application.Features.PasswordReset.Validation;

public sealed class RequestPasswordResetRequestValidator
    : DataAnnotationsValidator<RequestPasswordResetRequest>;
