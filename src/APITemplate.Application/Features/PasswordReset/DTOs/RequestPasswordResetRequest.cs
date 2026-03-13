using System.ComponentModel.DataAnnotations;
using APITemplate.Application.Common.Validation;

namespace APITemplate.Application.Features.PasswordReset.DTOs;

public sealed record RequestPasswordResetRequest(
    [NotEmpty] [MaxLength(320)] [EmailAddress] string Email
);
