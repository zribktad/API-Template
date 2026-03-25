using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using Wolverine;

namespace APITemplate.Application.Features.Tenant;

public sealed record DeleteTenantCommand(Guid Id) : IHasId;

public sealed class DeleteTenantCommandHandler
{
    public static async Task<(ErrorOr<Success>, OutgoingMessages)> HandleAsync(
        DeleteTenantCommand command,
        ITenantRepository repository,
        IUnitOfWork unitOfWork,
        IActorProvider actorProvider,
        TimeProvider timeProvider,
        CancellationToken ct
    )
    {
        var tenantResult = await repository.GetByIdOrError(
            command.Id,
            DomainErrors.Tenants.NotFound(command.Id),
            ct
        );
        if (tenantResult.IsError)
            return (tenantResult.Errors, []);

        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await repository.DeleteAsync(tenantResult.Value, ct);
            },
            ct
        );

        return (
            Result.Success,
            [
                new TenantSoftDeletedNotification(
                    command.Id,
                    actorProvider.ActorId,
                    timeProvider.GetUtcNow().UtcDateTime
                ),
                new CacheInvalidationNotification(CacheTags.Tenants),
            ]
        );
    }
}
