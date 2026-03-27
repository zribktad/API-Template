using Contracts.IntegrationEvents.ProductCatalog;
using Contracts.IntegrationEvents.Sagas;
using ProductCatalog.Application.Sagas;
using Shouldly;
using Xunit;

namespace ProductCatalog.Tests.Sagas;

public sealed class ProductDeletionSagaTests
{
    [Fact]
    public void Start_CreatesSagaWithCorrectProperties()
    {
        Guid correlationId = Guid.NewGuid();
        IReadOnlyList<Guid> productIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        Guid tenantId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        StartProductDeletionSaga command = new(correlationId, productIds, tenantId, actorId);

        (ProductDeletionSaga saga, ProductDeletedIntegrationEvent @event) =
            ProductDeletionSaga.Start(command, TimeProvider.System);

        saga.Id.ShouldBe(correlationId.ToString());
        saga.ProductIds.ShouldBe(productIds);
        saga.TenantId.ShouldBe(tenantId);
        saga.ReviewsCascaded.ShouldBeFalse();
        saga.FilesCascaded.ShouldBeFalse();
    }

    [Fact]
    public void Start_PublishesIntegrationEventWithCorrectFields()
    {
        IReadOnlyList<Guid> productIds = new[] { Guid.NewGuid() };
        Guid tenantId = Guid.NewGuid();
        StartProductDeletionSaga command = new(
            Guid.NewGuid(),
            productIds,
            tenantId,
            Guid.NewGuid()
        );

        (ProductDeletionSaga _, ProductDeletedIntegrationEvent @event) = ProductDeletionSaga.Start(
            command,
            TimeProvider.System
        );

        @event.ProductIds.ShouldBe(productIds);
        @event.TenantId.ShouldBe(tenantId);
        @event.CorrelationId.ShouldBe(command.CorrelationId);
    }

    [Fact]
    public void Handle_ReviewsCascadeCompleted_SetsFlag()
    {
        ProductDeletionSaga saga = CreateSaga();

        saga.Handle(new ReviewsCascadeCompleted(Guid.NewGuid(), 10));

        saga.ReviewsCascaded.ShouldBeTrue();
        saga.FilesCascaded.ShouldBeFalse();
    }

    [Fact]
    public void Handle_FilesCascadeCompleted_SetsFlag()
    {
        ProductDeletionSaga saga = CreateSaga();

        saga.Handle(new FilesCascadeCompleted(Guid.NewGuid(), 5));

        saga.FilesCascaded.ShouldBeTrue();
        saga.ReviewsCascaded.ShouldBeFalse();
    }

    [Fact]
    public void TryComplete_DoesNotComplete_WhenOnlyReviewsCascaded()
    {
        ProductDeletionSaga saga = CreateSaga();

        saga.Handle(new ReviewsCascadeCompleted(Guid.NewGuid(), 10));

        saga.IsCompleted().ShouldBeFalse();
    }

    [Fact]
    public void TryComplete_CompletesWhenBothCascaded()
    {
        ProductDeletionSaga saga = CreateSaga();

        saga.Handle(new ReviewsCascadeCompleted(Guid.NewGuid(), 10));
        saga.Handle(new FilesCascadeCompleted(Guid.NewGuid(), 5));

        saga.IsCompleted().ShouldBeTrue();
    }

    private static ProductDeletionSaga CreateSaga() =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            ProductIds = new[] { Guid.NewGuid() },
            TenantId = Guid.NewGuid(),
        };
}
