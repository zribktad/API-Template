using APITemplate.Application.Common.Validation;
using APITemplate.Application.Features.Examples.DTOs;

namespace APITemplate.Application.Features.Examples.Validation;

public sealed class IdempotentCreateRequestValidator
    : DataAnnotationsValidator<IdempotentCreateRequest>;
