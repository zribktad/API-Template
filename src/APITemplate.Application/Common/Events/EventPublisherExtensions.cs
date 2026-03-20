using Microsoft.Extensions.Logging;

namespace APITemplate.Application.Common.Events;

/// <summary>
/// Extension methods for <see cref="IEventPublisher"/> providing safe (fire-and-forget) publishing.
/// </summary>
public static class EventPublisherExtensions
{
    /// <summary>
    /// Publishes a domain event, swallowing any non-cancellation exception and logging it as a warning.
    /// Use for notification events whose failure must not break the main command flow.
    /// </summary>
    public static async Task PublishSafeAsync<TEvent>(
        this IEventPublisher publisher,
        TEvent @event,
        ILogger logger,
        CancellationToken ct
    )
        where TEvent : IDomainEvent
    {
        try
        {
            await publisher.PublishAsync(@event, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to publish {EventType}.", typeof(TEvent).Name);
        }
    }
}
