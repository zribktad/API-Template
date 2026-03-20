namespace APITemplate.Application.Common.Events;

/// <summary>Publishes domain events to all registered handlers.</summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct)
        where TEvent : IDomainEvent;
}
