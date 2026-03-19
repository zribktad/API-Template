using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Product;
using APITemplate.Application.Features.Product.Mappings;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Domain.Interfaces;
using FluentValidation;
using MediatR;
using DomainNotFoundException = APITemplate.Domain.Exceptions.NotFoundException;
using DomainValidationException = APITemplate.Domain.Exceptions.ValidationException;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Examples.Handlers;

/// <summary>
/// Accepts a delegate that applies patch operations to a <see cref="PatchableProductDto"/>.
/// This keeps the Application layer independent of any specific JSON Patch library.
/// </summary>
public sealed record PatchProductCommand(Guid Id, Action<PatchableProductDto> ApplyPatch)
    : IRequest<ProductResponse>;

/// <summary>
/// Application-layer handler for JSON Patch product updates; applies the caller-supplied patch delegate to a <see cref="PatchableProductDto"/>, validates the result, updates the domain entity in a transaction, and publishes a change notification.
/// </summary>
public sealed class PatchRequestHandlers : IRequestHandler<PatchProductCommand, ProductResponse>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<PatchableProductDto> _validator;
    private readonly IPublisher _publisher;

    public PatchRequestHandlers(
        IProductRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<PatchableProductDto> validator,
        IPublisher publisher
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _publisher = publisher;
    }

    /// <summary>Loads the product, applies patch operations via the command delegate, validates the mutated DTO, persists the changes, and returns the updated response.</summary>
    public async Task<ProductResponse> Handle(PatchProductCommand command, CancellationToken ct)
    {
        var product =
            await _repository.GetByIdAsync(command.Id, ct)
            ?? throw new DomainNotFoundException(
                nameof(ProductEntity),
                command.Id,
                ErrorCatalog.Products.NotFound
            );

        var dto = new PatchableProductDto
        {
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CategoryId = product.CategoryId,
        };

        command.ApplyPatch(dto);

        var validationResult = await _validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new DomainValidationException(
                string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
                ErrorCatalog.Examples.InvalidPatchDocument
            );

        product.UpdateDetails(dto.Name, dto.Description, dto.Price, dto.CategoryId);

        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await _repository.UpdateAsync(product, ct);
            },
            ct
        );

        await _publisher.Publish(new ProductsChangedNotification(), ct);

        return product.ToResponse();
    }
}
