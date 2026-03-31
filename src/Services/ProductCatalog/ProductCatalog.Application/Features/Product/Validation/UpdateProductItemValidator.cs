using ProductCatalog.Application.Features.Product.DTOs;
using SharedKernel.Application.Validation;

namespace ProductCatalog.Application.Features.Product.Validation;

/// <summary>
/// FluentValidation validator for <see cref="UpdateProductItem"/>, reusing the shared
/// product validation rules including the description-required-above-price-threshold rule.
/// </summary>
public sealed class UpdateProductItemValidator : ProductRequestValidatorBase<UpdateProductItem>;
