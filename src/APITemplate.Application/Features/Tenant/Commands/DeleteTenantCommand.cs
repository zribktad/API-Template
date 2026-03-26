using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using Wolverine;
using TenantEntity = APITemplate.Domain.Entities.Tenant;

namespace APITemplate.Application.Features.Tenant;

/// <summary>Soft-deletes a tenant and triggers cascading cleanup.</summary>
public sealed record DeleteTenantCommand(Guid Id) : IHasId;

public sealed class DeleteTenantCommandHandler
{
    public static async Task<(HandlerContinuation, TenantEntity?, OutgoingMessages)> LoadAsync(
        DeleteTenantCommand command,
        ITenantRepository repository,
        CancellationToken ct
    )
    {
        var tenantResult = await repository.GetByIdOrError(
            command.Id,
            DomainErrors.Tenants.NotFound(command.Id),
            ct
        );

        OutgoingMessages messages = new();

        if (tenantResult.IsError)
        {
            messages.RespondToSender((ErrorOr<Success>)tenantResult.Errors);
            return (HandlerContinuation.Stop, null, messages);
        }

        return (HandlerContinuation.Continue, tenantResult.Value, messages);
    }

    public static async Task<(ErrorOr<Success>, OutgoingMessages)> HandleAsync(
        DeleteTenantCommand command,
        TenantEntity tenant,
        ITenantRepository repository,
        IUnitOfWork unitOfWork,
        IActorProvider actorProvider,
        TimeProvider timeProvider,
        CancellationToken ct
    )
    {
        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await repository.DeleteAsync(tenant, ct);
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
