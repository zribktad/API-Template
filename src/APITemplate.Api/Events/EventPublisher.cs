using APITemplate.Application.Common.Events;

namespace APITemplate.Api.Events;

/// <summary>
/// Infrastructure implementation of <see cref="IEventPublisher"/> that resolves handlers from the DI container
/// and invokes them sequentially.
/// </summary>
public sealed class EventPublisher : IEventPublisher
{
    private readonly IServiceProvider _serviceProvider;

    public EventPublisher(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct)
        where TEvent : IDomainEvent
    {
        var handlers = _serviceProvider.GetServices<IDomainEventHandler<TEvent>>();
        foreach (var handler in handlers)
            await handler.HandleAsync(@event, ct);
    }
}
