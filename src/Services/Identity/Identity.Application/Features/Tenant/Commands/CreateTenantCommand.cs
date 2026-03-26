using ErrorOr;
using Identity.Application.Errors;
using Identity.Application.Features.Tenant.DTOs;
using Identity.Application.Features.Tenant.Mappings;
using Identity.Domain.Interfaces;
using SharedKernel.Domain.Interfaces;
using TenantEntity = Identity.Domain.Entities.Tenant;

namespace Identity.Application.Features.Tenant.Commands;

public sealed record CreateTenantCommand(CreateTenantRequest Request);

public sealed class CreateTenantCommandHandler
{
    public static async Task<ErrorOr<TenantResponse>> HandleAsync(
        CreateTenantCommand command,
        ITenantRepository repository,
        IUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        if (await repository.CodeExistsAsync(command.Request.Code, ct))
            return DomainErrors.Tenants.CodeAlreadyExists(command.Request.Code);

        TenantEntity tenant = await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                Guid id = Guid.NewGuid();
                TenantEntity entity = new()
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

        return tenant.ToResponse();
    }
}
