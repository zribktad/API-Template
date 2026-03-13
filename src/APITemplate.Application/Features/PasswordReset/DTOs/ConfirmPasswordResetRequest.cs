using System.ComponentModel.DataAnnotations;
using APITemplate.Application.Common.Validation;

namespace APITemplate.Application.Features.PasswordReset.DTOs;

public sealed record ConfirmPasswordResetRequest(
    [NotEmpty] string Token,
    [NotEmpty] [MinLength(8)] [MaxLength(128)] string NewPassword
);
