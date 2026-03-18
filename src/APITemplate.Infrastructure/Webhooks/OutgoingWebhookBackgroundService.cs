using System.Net.Http.Headers;
using System.Text;
using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Features.Examples.DTOs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace APITemplate.Infrastructure.Webhooks;

public sealed class OutgoingWebhookBackgroundService : BackgroundService
{
    private readonly IOutgoingWebhookQueueReader _queue;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebhookPayloadSigner _signer;
    private readonly ILogger<OutgoingWebhookBackgroundService> _logger;

    public OutgoingWebhookBackgroundService(
        IOutgoingWebhookQueueReader queue,
        IHttpClientFactory httpClientFactory,
        IWebhookPayloadSigner signer,
        ILogger<OutgoingWebhookBackgroundService> logger
    )
    {
        _queue = queue;
        _httpClientFactory = httpClientFactory;
        _signer = signer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await SendWebhookAsync(item, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Failed to deliver outgoing webhook to {Url}",
                    item.CallbackUrl
                );
            }
        }
    }

    private async Task SendWebhookAsync(OutgoingWebhookItem item, CancellationToken ct)
    {
        var signatureResult = _signer.Sign(item.SerializedPayload);

        using var client = _httpClientFactory.CreateClient(WebhookConstants.OutgoingHttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, item.CallbackUrl)
        {
            Content = new StringContent(
                item.SerializedPayload,
                Encoding.UTF8,
                new MediaTypeHeaderValue("application/json")
            ),
        };

        request.Headers.Add(WebhookConstants.SignatureHeader, signatureResult.Signature);
        request.Headers.Add(WebhookConstants.TimestampHeader, signatureResult.Timestamp);

        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Outgoing webhook delivered to {Url}", item.CallbackUrl);
    }
}
