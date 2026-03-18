using System.ComponentModel.DataAnnotations;
using APITemplate.Application.Common.Validation;

namespace APITemplate.Application.Features.Examples.DTOs;

public sealed record IdempotentCreateRequest(
    [NotEmpty] [MaxLength(200)] string Name,
    string? Description
);
