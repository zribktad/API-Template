using APITemplate.Application.Features.Examples.DTOs;

namespace APITemplate.Application.Common.BackgroundJobs;

public interface IOutgoingWebhookQueue : IQueue<OutgoingWebhookItem>;

public interface IOutgoingWebhookQueueReader : IQueueReader<OutgoingWebhookItem>;
