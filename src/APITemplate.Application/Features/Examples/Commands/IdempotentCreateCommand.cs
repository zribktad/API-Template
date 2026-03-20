using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Domain.Interfaces;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Examples;

public sealed record IdempotentCreateCommand(IdempotentCreateRequest Request)
    : ICommand<IdempotentCreateResponse>;

public sealed class IdempotentCreateCommandHandler
    : ICommandHandler<IdempotentCreateCommand, IdempotentCreateResponse>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public IdempotentCreateCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IdempotentCreateResponse> HandleAsync(
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
