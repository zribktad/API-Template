using Contracts.IntegrationEvents.ProductCatalog;
using Contracts.IntegrationEvents.Sagas;
using Wolverine;

namespace ProductCatalog.Application.Sagas;

/// <summary>
/// Wolverine saga that coordinates the product deletion cascade across services.
/// Waits for both Reviews and File Storage to confirm cascade completion before finishing.
/// </summary>
public class ProductDeletionSaga : Saga
{
    public string? Id { get; set; }
    public IReadOnlyList<Guid> ProductIds { get; set; } = [];
    public Guid TenantId { get; set; }
    public bool ReviewsCascaded { get; set; }
    public bool FilesCascaded { get; set; }

    /// <summary>
    /// Starts the saga and publishes the <see cref="ProductDeletedIntegrationEvent"/> to trigger downstream cascades.
    /// </summary>
    public static (ProductDeletionSaga, ProductDeletedIntegrationEvent) Start(
        StartProductDeletionSaga command,
        TimeProvider timeProvider
    )
    {
        ProductDeletionSaga saga = new()
        {
            Id = command.CorrelationId.ToString(),
            ProductIds = command.ProductIds,
            TenantId = command.TenantId,
        };

        ProductDeletedIntegrationEvent integrationEvent = new(
            command.CorrelationId,
            command.ProductIds,
            command.TenantId,
            timeProvider.GetUtcNow().UtcDateTime
        );

        return (saga, integrationEvent);
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

    private void TryComplete()
    {
        if (ReviewsCascaded && FilesCascaded)
            MarkCompleted();
    }
}
