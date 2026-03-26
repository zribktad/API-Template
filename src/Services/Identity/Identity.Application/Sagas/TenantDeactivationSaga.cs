using Contracts.IntegrationEvents.Identity;
using Contracts.IntegrationEvents.Sagas;
using Wolverine;

namespace Identity.Application.Sagas;

public class TenantDeactivationSaga : Saga
{
    public string? Id { get; set; }
    public Guid TenantId { get; set; }
    public bool UsersCascaded { get; set; }
    public bool ProductsCascaded { get; set; }
    public bool CategoriesCascaded { get; set; }

    public static (TenantDeactivationSaga, TenantDeactivatedIntegrationEvent) Start(
        StartTenantDeactivationSaga command
    )
    {
        TenantDeactivationSaga saga = new()
        {
            Id = command.CorrelationId.ToString(),
            TenantId = command.TenantId,
        };
        TenantDeactivatedIntegrationEvent @event = new(
            command.TenantId,
            command.ActorId,
            DateTime.UtcNow
        );
        return (saga, @event);
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

    private void TryComplete()
    {
        if (UsersCascaded && ProductsCascaded && CategoriesCascaded)
            MarkCompleted();
    }
}
