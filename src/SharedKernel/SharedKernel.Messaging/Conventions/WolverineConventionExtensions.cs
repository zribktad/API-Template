using Wolverine;

namespace SharedKernel.Messaging.Conventions;

/// <summary>
/// Extension methods that apply shared Wolverine conventions for durable messaging
/// across all microservices in the system.
/// </summary>
public static class WolverineConventionExtensions
{
    /// <summary>
    /// Applies shared durability and outbox/inbox conventions to Wolverine messaging options.
    /// </summary>
    public static WolverineOptions ApplySharedConventions(this WolverineOptions opts)
    {
        opts.Durability.Mode = DurabilityMode.Balanced;
        opts.Policies.UseDurableInboxOnAllListeners();
        opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
        return opts;
    }
}
