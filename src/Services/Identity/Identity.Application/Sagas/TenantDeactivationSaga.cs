using Contracts.IntegrationEvents.Identity;
using Contracts.IntegrationEvents.Sagas;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Identity.Application.Sagas;

public class TenantDeactivationSaga : Saga
{
    public string? Id { get; set; }
    public Guid TenantId { get; set; }
    public bool UsersCascaded { get; set; }
    public bool ProductsCascaded { get; set; }
    public bool CategoriesCascaded { get; set; }

    public static (
        TenantDeactivationSaga,
        TenantDeactivatedIntegrationEvent,
        TenantDeactivationSagaTimeout
    ) Start(StartTenantDeactivationSaga command, TimeProvider timeProvider)
    {
        TenantDeactivationSaga saga = new()
        {
            Id = command.CorrelationId.ToString(),
            TenantId = command.TenantId,
        };
        TenantDeactivatedIntegrationEvent @event = new(
            command.CorrelationId,
            command.TenantId,
            command.ActorId,
            timeProvider.GetUtcNow().UtcDateTime
        );

        TenantDeactivationSagaTimeout timeout = new(command.CorrelationId);
        return (saga, @event, timeout);
    }

    public void Handle(UsersCascadeCompleted _)
    {
        UsersCascaded = true;
        TryComplete();
    }

    public void Handle(ProductsCascadeCompleted _)
    {
        ProductsCascaded = true;
        TryComplete();
    }

    public void Handle(CategoriesCascadeCompleted _)
    {
        CategoriesCascaded = true;
        TryComplete();
    }

    public void Handle(
        TenantDeactivationSagaTimeout timeout,
        ILogger<TenantDeactivationSaga> logger
    )
    {
        if (UsersCascaded && ProductsCascaded && CategoriesCascaded)
            return;

        logger.LogWarning(
            "TenantDeactivationSaga timed out for {SagaId}. Pending confirmations: UsersCascaded={UsersCascaded}, ProductsCascaded={ProductsCascaded}, CategoriesCascaded={CategoriesCascaded}, TenantId={TenantId}",
            timeout.CorrelationId,
            UsersCascaded,
            ProductsCascaded,
            CategoriesCascaded,
            TenantId
        );

        MarkCompleted();
    }

    private void TryComplete()
    {
        if (UsersCascaded && ProductsCascaded && CategoriesCascaded)
            MarkCompleted();
    }

    public static void NotFound(
        UsersCascadeCompleted msg,
        ILogger<TenantDeactivationSaga> logger
    ) =>
        logger.LogWarning(
            "Received {MessageType} for unknown saga {SagaId}",
            nameof(UsersCascadeCompleted),
            msg.CorrelationId
        );

    public static void NotFound(
        ProductsCascadeCompleted msg,
        ILogger<TenantDeactivationSaga> logger
    ) =>
        logger.LogWarning(
            "Received {MessageType} for unknown saga {SagaId}",
            nameof(ProductsCascadeCompleted),
            msg.CorrelationId
        );

    public static void NotFound(
        CategoriesCascadeCompleted msg,
        ILogger<TenantDeactivationSaga> logger
    ) =>
        logger.LogWarning(
            "Received {MessageType} for unknown saga {SagaId}",
            nameof(CategoriesCascadeCompleted),
            msg.CorrelationId
        );

    public static void NotFound(
        TenantDeactivationSagaTimeout msg,
        ILogger<TenantDeactivationSaga> logger
    ) =>
        logger.LogInformation(
            "Received {MessageType} for already-completed or missing saga {SagaId}",
            nameof(TenantDeactivationSagaTimeout),
            msg.CorrelationId
        );
}
