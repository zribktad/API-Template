using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Webhooks.Application.Common.Constants;
using Webhooks.Application.Common.Contracts;
using Webhooks.Domain.Entities;
using Webhooks.Domain.Interfaces;

namespace Webhooks.Infrastructure.Delivery;

/// <summary>
/// Delivers webhook payloads to all active subscribers registered for a given event type,
/// signing each payload with the subscriber's own HMAC secret and logging delivery results.
/// </summary>
public sealed class WebhookDeliveryService : IWebhookDeliveryService
{
    private readonly IWebhookSubscriptionRepository _subscriptionRepository;
    private readonly IWebhookDeliveryLogRepository _deliveryLogRepository;
    private readonly IWebhookPayloadSigner _signer;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDeliveryService> _logger;

    public WebhookDeliveryService(
        IWebhookSubscriptionRepository subscriptionRepository,
        IWebhookDeliveryLogRepository deliveryLogRepository,
        IWebhookPayloadSigner signer,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDeliveryService> logger
    )
    {
        _subscriptionRepository = subscriptionRepository;
        _deliveryLogRepository = deliveryLogRepository;
        _signer = signer;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task DeliverAsync(
        string eventType,
        string serializedPayload,
        CancellationToken ct = default
    )
    {
        IReadOnlyList<WebhookSubscription> subscriptions =
            await _subscriptionRepository.GetActiveByEventTypeAsync(eventType, ct);

        foreach (WebhookSubscription subscription in subscriptions)
        {
            await DeliverToSubscriberAsync(subscription, eventType, serializedPayload, ct);
        }
    }

    private async Task DeliverToSubscriberAsync(
        WebhookSubscription subscription,
        string eventType,
        string serializedPayload,
        CancellationToken ct
    )
    {
        WebhookDeliveryLog log = new()
        {
            Id = Guid.NewGuid(),
            WebhookSubscriptionId = subscription.Id,
            EventType = eventType,
            Payload = serializedPayload,
            AttemptedAtUtc = DateTime.UtcNow,
        };

        try
        {
            WebhookSignatureResult signatureResult = _signer.Sign(
                serializedPayload,
                subscription.Secret
            );

            using HttpClient client = _httpClientFactory.CreateClient(
                WebhookConstants.OutgoingHttpClientName
            );
            using HttpRequestMessage request = new(HttpMethod.Post, subscription.Url)
            {
                Content = new StringContent(
                    serializedPayload,
                    Encoding.UTF8,
                    new MediaTypeHeaderValue("application/json")
                ),
            };

            request.Headers.Add(WebhookConstants.SignatureHeader, signatureResult.Signature);
            request.Headers.Add(WebhookConstants.TimestampHeader, signatureResult.Timestamp);

            using HttpResponseMessage response = await client.SendAsync(request, ct);

            log.HttpStatusCode = (int)response.StatusCode;
            log.Success = response.IsSuccessStatusCode;

            if (!response.IsSuccessStatusCode)
            {
                log.Error = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                _logger.LogWarning(
                    "Webhook delivery to {Url} returned {StatusCode}",
                    subscription.Url,
                    response.StatusCode
                );
            }
            else
            {
                _logger.LogInformation("Webhook delivered to {Url}", subscription.Url);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.Success = false;
            log.Error =
                ex.Message.Length > WebhookDeliveryLog.ErrorMaxLength
                    ? ex.Message[..WebhookDeliveryLog.ErrorMaxLength]
                    : ex.Message;

            _logger.LogError(ex, "Failed to deliver webhook to {Url}", subscription.Url);
        }

        await _deliveryLogRepository.AddAsync(log, ct);
        await _deliveryLogRepository.SaveChangesAsync(ct);
    }
}
