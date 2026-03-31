using Contracts.IntegrationEvents.ProductCatalog;
using Contracts.IntegrationEvents.Sagas;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace ProductCatalog.Application.Sagas;

/// <summary>
/// Wolverine saga that coordinates the product deletion cascade across services.
/// Waits for both Reviews and File Storage to confirm cascade completion before finishing.
/// </summary>
public class ProductDeletionSaga : Saga
{
    public Guid Id { get; set; }
    public IReadOnlyList<Guid> ProductIds { get; set; } = [];
    public Guid TenantId { get; set; }
    public bool ReviewsCascaded { get; set; }
    public bool FilesCascaded { get; set; }

    /// <summary>
    /// Starts the saga and publishes the <see cref="ProductDeletedIntegrationEvent"/> to trigger downstream cascades.
    /// </summary>
    public static (
        ProductDeletionSaga,
        ProductDeletedIntegrationEvent,
        ProductDeletionSagaTimeout
    ) Start(StartProductDeletionSaga command, TimeProvider timeProvider)
    {
        ProductDeletionSaga saga = new()
        {
            Id = command.CorrelationId,
            ProductIds = command.ProductIds,
            TenantId = command.TenantId,
        };

        ProductDeletedIntegrationEvent integrationEvent = new(
            command.CorrelationId,
            command.ProductIds,
            command.TenantId,
            timeProvider.GetUtcNow().UtcDateTime
        );

        ProductDeletionSagaTimeout timeout = new(command.CorrelationId);

        return (saga, integrationEvent, timeout);
    }

    /// <summary>
    /// Handles confirmation that reviews have been cascade-deleted.
    /// </summary>
    public void Handle(ReviewsCascadeCompleted message)
    {
        ReviewsCascaded = true;
        TryComplete();
    }

    /// <summary>
    /// Handles confirmation that files have been orphaned.
    /// </summary>
    public void Handle(FilesCascadeCompleted message)
    {
        FilesCascaded = true;
        TryComplete();
    }

    /// <summary>
    /// Handles timeout when downstream services do not complete the cascade in time.
    /// </summary>
    public void Handle(ProductDeletionSagaTimeout timeout, ILogger<ProductDeletionSaga> logger)
    {
        if (ReviewsCascaded && FilesCascaded)
            return;

        logger.LogWarning(
            "ProductDeletionSaga timed out for {SagaId}. Pending confirmations: ReviewsCascaded={ReviewsCascaded}, FilesCascaded={FilesCascaded}, TenantId={TenantId}",
            timeout.CorrelationId,
            ReviewsCascaded,
            FilesCascaded,
            TenantId
        );

        MarkCompleted();
    }

    private void TryComplete()
    {
        if (ReviewsCascaded && FilesCascaded)
            MarkCompleted();
    }

    public static void NotFound(ReviewsCascadeCompleted msg, ILogger<ProductDeletionSaga> logger) =>
        logger.LogWarning(
            "Received {MessageType} for unknown saga {SagaId}",
            nameof(ReviewsCascadeCompleted),
            msg.CorrelationId
        );

    public static void NotFound(FilesCascadeCompleted msg, ILogger<ProductDeletionSaga> logger) =>
        logger.LogWarning(
            "Received {MessageType} for unknown saga {SagaId}",
            nameof(FilesCascadeCompleted),
            msg.CorrelationId
        );

    public static void NotFound(
        ProductDeletionSagaTimeout msg,
        ILogger<ProductDeletionSaga> logger
    ) =>
        logger.LogInformation(
            "Received {MessageType} for already-completed or missing saga {SagaId}",
            nameof(ProductDeletionSagaTimeout),
            msg.CorrelationId
        );
}
