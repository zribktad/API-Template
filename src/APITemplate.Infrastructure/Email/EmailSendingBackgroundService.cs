using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Resilience;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly.Registry;

namespace APITemplate.Infrastructure.Email;

public sealed class EmailSendingBackgroundService : BackgroundService
{
    private readonly IEmailQueueReader _queue;
    private readonly IEmailSender _sender;
    private readonly ResiliencePipelineProvider<string> _resiliencePipelineProvider;
    private readonly IFailedEmailStore _failedEmailStore;
    private readonly ILogger<EmailSendingBackgroundService> _logger;

    public EmailSendingBackgroundService(
        IEmailQueueReader queue,
        IEmailSender sender,
        ResiliencePipelineProvider<string> resiliencePipelineProvider,
        IFailedEmailStore failedEmailStore,
        ILogger<EmailSendingBackgroundService> logger
    )
    {
        _queue = queue;
        _sender = sender;
        _resiliencePipelineProvider = resiliencePipelineProvider;
        _failedEmailStore = failedEmailStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pipeline = _resiliencePipelineProvider.GetPipeline(ResiliencePipelineKeys.SmtpSend);

        await foreach (var message in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await pipeline.ExecuteAsync(
                    async token =>
                    {
                        await _sender.SendAsync(message, token);
                    },
                    stoppingToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send email to {Recipient} with subject '{Subject}' after all retry attempts.",
                    message.To,
                    message.Subject
                );

                await _failedEmailStore.StoreFailedAsync(message, ex.Message, stoppingToken);
            }
        }
    }
}
