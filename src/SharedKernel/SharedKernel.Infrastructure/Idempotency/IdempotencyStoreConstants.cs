namespace SharedKernel.Infrastructure.Idempotency;

/// <summary>Shared key-naming constants used by idempotency store implementations.</summary>
internal static class IdempotencyStoreConstants
{
    /// <summary>Suffix appended to an idempotency key to form the corresponding distributed-lock key.</summary>
    public const string LockSuffix = ":lock";
}
