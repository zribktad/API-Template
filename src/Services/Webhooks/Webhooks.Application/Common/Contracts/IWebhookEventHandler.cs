namespace Webhooks.Application.Common.Contracts;

/// <summary>
/// Application-layer abstraction for handling inbound webhook events after they have been
/// validated and dequeued by the background processing pipeline.
/// </summary>
public interface IWebhookEventHandler
{
    /// <summary>
    /// Processes a single inbound webhook event identified by <paramref name="eventType"/>.
    /// </summary>
    Task HandleAsync(
        string eventType,
        string payload,
        CancellationToken cancellationToken = default
    );
}
