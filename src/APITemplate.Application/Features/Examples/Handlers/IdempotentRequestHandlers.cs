using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Domain.Interfaces;
using MediatR;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Examples.Handlers;

/// <summary>Creates a new resource using the data supplied in the inner <see cref="IdempotentCreateRequest"/>; intended to demonstrate idempotent command handling at the API layer.</summary>
public sealed record IdempotentCreateCommand(IdempotentCreateRequest Request)
    : IRequest<IdempotentCreateResponse>;

/// <summary>
/// Application-layer handler that creates a product-like entity inside a transaction, demonstrating idempotency key patterns at the presentation layer.
/// </summary>
public sealed class IdempotentRequestHandlers
    : IRequestHandler<IdempotentCreateCommand, IdempotentCreateResponse>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public IdempotentRequestHandlers(IProductRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Persists the new entity in a transaction and maps it to an <see cref="IdempotentCreateResponse"/>.</summary>
    public async Task<IdempotentCreateResponse> Handle(
        IdempotentCreateCommand command,
        CancellationToken ct
    )
    {
        var entity = new ProductEntity
        {
            Id = Guid.NewGuid(),
            Name = command.Request.Name,
            Description = command.Request.Description,
            Price = 0,
        };

        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await _repository.AddAsync(entity, ct);
            },
            ct
        );

        return new IdempotentCreateResponse(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.Audit.CreatedAtUtc
        );
    }
}
