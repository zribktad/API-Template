using APITemplate.Application.Features.Examples.DTOs;

namespace APITemplate.Application.Common.BackgroundJobs;

public interface IWebhookProcessingQueue : IQueue<WebhookPayload>;

public interface IWebhookQueueReader : IQueueReader<WebhookPayload>;
