using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Product;
using APITemplate.Application.Features.Product.Mappings;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using FluentValidation;
using Wolverine;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Examples;

/// <summary>Partially updates a product using a JSON Patch document.</summary>
public sealed record PatchProductCommand(Guid Id, Action<PatchableProductDto> ApplyPatch) : IHasId;

public sealed class PatchProductCommandHandler
{
    public static async Task<(HandlerContinuation, ProductEntity?, OutgoingMessages)> LoadAsync(
        PatchProductCommand command,
        IProductRepository repository,
        CancellationToken ct
    )
    {
        var product = await repository.GetByIdAsync(command.Id, ct);

        OutgoingMessages messages = new();

        if (product is null)
        {
            messages.RespondToSender(
                (ErrorOr<ProductResponse>)DomainErrors.Products.NotFound(command.Id)
            );
            return (HandlerContinuation.Stop, null, messages);
        }

        return (HandlerContinuation.Continue, product, messages);
    }

    public static async Task<(ErrorOr<ProductResponse>, OutgoingMessages)> HandleAsync(
        PatchProductCommand command,
        ProductEntity product,
        IProductRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<PatchableProductDto> validator,
        CancellationToken ct
    )
    {
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
            return (
                DomainErrors.Examples.InvalidPatchDocument(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))
                ),
                []
            );

        product.UpdateDetails(dto.Name, dto.Description, dto.Price, dto.CategoryId);

        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await repository.UpdateAsync(product, ct);
            },
            ct
        );

        return (product.ToResponse(), [new CacheInvalidationNotification(CacheTags.Products)]);
    }
}
