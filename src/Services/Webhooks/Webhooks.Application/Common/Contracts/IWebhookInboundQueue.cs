using SharedKernel.Application.Queue;
using Webhooks.Application.Common.DTOs;

namespace Webhooks.Application.Common.Contracts;

/// <summary>
/// Marker interface for the inbound webhook queue, allowing DI registration
/// with a service-specific contract while reusing the shared queue abstraction.
/// </summary>
public interface IWebhookInboundQueue : IQueue<InboundWebhookMessage>;
