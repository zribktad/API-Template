using Contracts.IntegrationEvents.Identity;
using Contracts.IntegrationEvents.Sagas;
using Identity.Application.Sagas;
using Shouldly;
using Xunit;

namespace Identity.Tests.Sagas;

public sealed class TenantDeactivationSagaTests
{
    [Fact]
    public void Start_CreatesSagaWithCorrectProperties()
    {
        Guid correlationId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        StartTenantDeactivationSaga command = new(correlationId, tenantId, actorId);

        (
            TenantDeactivationSaga saga,
            TenantDeactivatedIntegrationEvent @event,
            TenantDeactivationSagaTimeout timeout
        ) = TenantDeactivationSaga.Start(command, TimeProvider.System);

        saga.Id.ShouldBe(correlationId.ToString());
        saga.TenantId.ShouldBe(tenantId);
        saga.UsersCascaded.ShouldBeFalse();
        saga.ProductsCascaded.ShouldBeFalse();
        saga.CategoriesCascaded.ShouldBeFalse();
        timeout.CorrelationId.ShouldBe(correlationId);
    }

    [Fact]
    public void Start_PublishesIntegrationEventWithCorrectFields()
    {
        Guid correlationId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        StartTenantDeactivationSaga command = new(correlationId, tenantId, actorId);

        (
            TenantDeactivationSaga _,
            TenantDeactivatedIntegrationEvent @event,
            TenantDeactivationSagaTimeout timeout
        ) = TenantDeactivationSaga.Start(command, TimeProvider.System);

        @event.CorrelationId.ShouldBe(correlationId);
        @event.TenantId.ShouldBe(tenantId);
        @event.ActorId.ShouldBe(actorId);
        timeout.CorrelationId.ShouldBe(correlationId);
    }

    [Fact]
    public void Handle_UsersCascadeCompleted_SetsFlag()
    {
        TenantDeactivationSaga saga = CreateSaga();

        saga.Handle(new UsersCascadeCompleted(Guid.NewGuid(), Guid.NewGuid(), 5));

        saga.UsersCascaded.ShouldBeTrue();
        saga.ProductsCascaded.ShouldBeFalse();
        saga.CategoriesCascaded.ShouldBeFalse();
    }

    [Fact]
    public void Handle_ProductsCascadeCompleted_SetsFlag()
    {
        TenantDeactivationSaga saga = CreateSaga();

        saga.Handle(new ProductsCascadeCompleted(Guid.NewGuid(), Guid.NewGuid(), 3));

        saga.ProductsCascaded.ShouldBeTrue();
    }

    [Fact]
    public void Handle_CategoriesCascadeCompleted_SetsFlag()
    {
        TenantDeactivationSaga saga = CreateSaga();

        saga.Handle(new CategoriesCascadeCompleted(Guid.NewGuid(), Guid.NewGuid(), 2));

        saga.CategoriesCascaded.ShouldBeTrue();
    }

    [Fact]
    public void TryComplete_DoesNotComplete_WhenNotAllCascaded()
    {
        TenantDeactivationSaga saga = CreateSaga();

        saga.Handle(new UsersCascadeCompleted(Guid.NewGuid(), Guid.NewGuid(), 5));
        saga.Handle(new ProductsCascadeCompleted(Guid.NewGuid(), Guid.NewGuid(), 3));

        saga.IsCompleted().ShouldBeFalse();
    }

    [Fact]
    public void TryComplete_CompletesWhenAllCascaded()
    {
        TenantDeactivationSaga saga = CreateSaga();

        saga.Handle(new UsersCascadeCompleted(Guid.NewGuid(), Guid.NewGuid(), 5));
        saga.Handle(new ProductsCascadeCompleted(Guid.NewGuid(), Guid.NewGuid(), 3));
        saga.Handle(new CategoriesCascadeCompleted(Guid.NewGuid(), Guid.NewGuid(), 2));

        saga.IsCompleted().ShouldBeTrue();
    }

    [Fact]
    public void Handle_Timeout_CompletesWhenCascadeIsIncomplete()
    {
        TenantDeactivationSaga saga = CreateSaga();

        saga.Handle(
            new TenantDeactivationSagaTimeout(Guid.Parse(saga.Id!)),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<TenantDeactivationSaga>.Instance
        );

        saga.IsCompleted().ShouldBeTrue();
    }

    private static TenantDeactivationSaga CreateSaga() =>
        new() { Id = Guid.NewGuid().ToString(), TenantId = Guid.NewGuid() };
}
