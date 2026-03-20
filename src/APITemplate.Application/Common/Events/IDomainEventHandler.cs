namespace APITemplate.Application.Common.Events;

/// <summary>Handles a domain event of type <typeparamref name="TEvent"/>.</summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct);
}
