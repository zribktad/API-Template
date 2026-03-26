using RabbitMQ.Client;
using Wolverine;
using Wolverine.RabbitMQ.Internal;

namespace SharedKernel.Messaging.Topology;

/// <summary>
/// Custom envelope mapper that propagates TenantId via RabbitMQ message headers,
/// enabling tenant-aware message routing across microservices.
/// </summary>
public sealed class TenantAwareEnvelopeMapper : IRabbitMqEnvelopeMapper
{
    private const string TenantIdHeader = "x-tenant-id";

    /// <summary>
    /// Maps the Wolverine envelope's TenantId to an outgoing RabbitMQ message header.
    /// </summary>
    public void MapEnvelopeToOutgoing(Envelope envelope, IBasicProperties outgoing)
    {
        if (envelope.TenantId is not null)
        {
            outgoing.Headers ??= new Dictionary<string, object?>();
            outgoing.Headers[TenantIdHeader] = envelope.TenantId;
        }
    }

    /// <summary>
    /// Maps the incoming RabbitMQ message's tenant header to the Wolverine envelope's TenantId.
    /// </summary>
    public void MapIncomingToEnvelope(Envelope envelope, IReadOnlyBasicProperties incoming)
    {
        if (
            incoming.Headers is not null
            && incoming.Headers.TryGetValue(TenantIdHeader, out object? tenantId)
            && tenantId is not null
        )
        {
            envelope.TenantId = tenantId switch
            {
                byte[] bytes => System.Text.Encoding.UTF8.GetString(bytes),
                _ => tenantId.ToString(),
            };
        }
    }
}
