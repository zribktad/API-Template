using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Tenant.DTOs;
using APITemplate.Application.Features.Tenant.Mappings;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using TenantEntity = APITemplate.Domain.Entities.Tenant;

namespace APITemplate.Application.Features.Tenant;

public sealed record CreateTenantCommand(CreateTenantRequest Request) : ICommand<TenantResponse>;

public sealed class CreateTenantCommandHandler
    : ICommandHandler<CreateTenantCommand, TenantResponse>
{
    private readonly ITenantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;

    public CreateTenantCommandHandler(
        ITenantRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<TenantResponse> HandleAsync(CreateTenantCommand command, CancellationToken ct)
    {
        if (await _repository.CodeExistsAsync(command.Request.Code, ct))
            throw new ConflictException(
                $"Tenant with code '{command.Request.Code}' already exists.",
                ErrorCatalog.Tenants.CodeAlreadyExists
            );

        var tenant = await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                var id = Guid.NewGuid();
                var entity = new TenantEntity
                {
                    Id = id,
                    TenantId = id,
                    Code = command.Request.Code,
                    Name = command.Request.Name,
                };

                await _repository.AddAsync(entity, ct);
                return entity;
            },
            ct
        );

        await _publisher.PublishAsync(new TenantsChangedNotification(), ct);
        return tenant.ToResponse();
    }
}
