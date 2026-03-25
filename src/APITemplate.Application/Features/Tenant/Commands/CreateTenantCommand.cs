using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Tenant.DTOs;
using APITemplate.Application.Features.Tenant.Mappings;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using Wolverine;
using TenantEntity = APITemplate.Domain.Entities.Tenant;

namespace APITemplate.Application.Features.Tenant;

public sealed record CreateTenantCommand(CreateTenantRequest Request);

public sealed class CreateTenantCommandHandler
{
    public static async Task<(ErrorOr<TenantResponse>, OutgoingMessages)> HandleAsync(
        CreateTenantCommand command,
        ITenantRepository repository,
        IUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        if (await repository.CodeExistsAsync(command.Request.Code, ct))
            return (DomainErrors.Tenants.CodeAlreadyExists(command.Request.Code), []);

        var tenant = await unitOfWork.ExecuteInTransactionAsync(
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

                await repository.AddAsync(entity, ct);
                return entity;
            },
            ct
        );

        return (tenant.ToResponse(), [new CacheInvalidationNotification(CacheTags.Tenants)]);
    }
}
