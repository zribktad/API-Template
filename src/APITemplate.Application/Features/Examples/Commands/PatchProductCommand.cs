using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Product;
using APITemplate.Application.Features.Product.Mappings;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Domain.Interfaces;
using FluentValidation;
using Wolverine;
using DomainNotFoundException = APITemplate.Domain.Exceptions.NotFoundException;
using DomainValidationException = APITemplate.Domain.Exceptions.ValidationException;

namespace APITemplate.Application.Features.Examples;

public sealed record PatchProductCommand(Guid Id, Action<PatchableProductDto> ApplyPatch) : IHasId;

public sealed class PatchProductCommandHandler
{
    public static async Task<ProductResponse> HandleAsync(
        PatchProductCommand command,
        IProductRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<PatchableProductDto> validator,
        IMessageBus bus,
        CancellationToken ct
    )
    {
        var product =
            await repository.GetByIdAsync(command.Id, ct)
            ?? throw new DomainNotFoundException(
                ErrorCatalog.Products.EntityName,
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

        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new DomainValidationException(
                string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
                ErrorCatalog.Examples.InvalidPatchDocument
            );

        product.UpdateDetails(dto.Name, dto.Description, dto.Price, dto.CategoryId);

        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await repository.UpdateAsync(product, ct);
            },
            ct
        );

        await bus.PublishAsync(new CacheInvalidationNotification(CacheTags.Products));

        return product.ToResponse();
    }
}
