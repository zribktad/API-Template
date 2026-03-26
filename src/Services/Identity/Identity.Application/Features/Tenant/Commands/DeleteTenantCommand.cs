using Contracts.IntegrationEvents.Sagas;
using ErrorOr;
using Identity.Application.Errors;
using Identity.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using SharedKernel.Application.Context;
using SharedKernel.Application.Extensions;
using SharedKernel.Domain.Entities.Contracts;
using SharedKernel.Domain.Interfaces;
using Wolverine;

namespace Identity.Application.Features.Tenant.Commands;

public sealed record DeleteTenantCommand(Guid Id) : IHasId;

public sealed class DeleteTenantCommandHandler
{
    public static async Task<ErrorOr<Success>> HandleAsync(
        DeleteTenantCommand command,
        ITenantRepository repository,
        IUnitOfWork unitOfWork,
        IMessageBus bus,
        IActorProvider actorProvider,
        ILogger<DeleteTenantCommandHandler> logger,
        CancellationToken ct
    )
    {
        ErrorOr<Domain.Entities.Tenant> tenantResult = await repository.GetByIdOrError(
            command.Id,
            DomainErrors.Tenants.NotFound(command.Id),
            ct
        );
        if (tenantResult.IsError)
            return tenantResult.Errors;

        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await repository.DeleteAsync(tenantResult.Value, ct);
            },
            ct
        );

        try
        {
            await bus.PublishAsync(
                new StartTenantDeactivationSaga(Guid.NewGuid(), command.Id, actorProvider.ActorId)
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Failed to publish {EventType}.",
                nameof(StartTenantDeactivationSaga)
            );
        }

        return Result.Success;
    }
}
