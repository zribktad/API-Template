using APITemplate.Application.Common.Validation;
using APITemplate.Application.Features.Examples.DTOs;

namespace APITemplate.Application.Features.Examples.Validation;

/// <summary>
/// FluentValidation validator for <see cref="BatchCreateProductsRequest"/> that enforces data-annotation constraints, including the 1–100 item count limit.
/// </summary>
public sealed class BatchCreateProductsRequestValidator
    : DataAnnotationsValidator<BatchCreateProductsRequest>;
