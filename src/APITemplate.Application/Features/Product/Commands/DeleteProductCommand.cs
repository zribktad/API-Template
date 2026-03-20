using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Application.Features.Product.Specifications;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Product;

/// <summary>Soft-deletes a product and its associated data links.</summary>
public sealed record DeleteProductCommand(Guid Id) : ICommand;

/// <summary>Handles <see cref="DeleteProductCommand"/> by soft-deleting data links and the product in a transaction.</summary>
public sealed class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;

    public DeleteProductCommandHandler(
        IProductRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task HandleAsync(DeleteProductCommand command, CancellationToken ct)
    {
        var product =
            await _repository.FirstOrDefaultAsync(
                new ProductByIdWithLinksSpecification(command.Id),
                ct
            )
            ?? throw new NotFoundException(
                nameof(ProductEntity),
                command.Id,
                ErrorCatalog.Products.NotFound
            );

        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                product.SoftDeleteProductDataLinks();
                await _repository.DeleteAsync(product, ct);
            },
            ct
        );

        await _publisher.PublishAsync(new ProductsChangedNotification(), ct);
        await _publisher.PublishAsync(new ProductReviewsChangedNotification(), ct);
    }
}
