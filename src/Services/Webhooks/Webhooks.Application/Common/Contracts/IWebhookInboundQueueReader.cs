using SharedKernel.Application.Queue;
using Webhooks.Application.Common.DTOs;

namespace Webhooks.Application.Common.Contracts;

/// <summary>
/// Marker interface for the inbound webhook queue reader, allowing the background
/// consumer to depend on a service-specific contract rather than the generic queue reader.
/// </summary>
public interface IWebhookInboundQueueReader : IQueueReader<InboundWebhookMessage>;
