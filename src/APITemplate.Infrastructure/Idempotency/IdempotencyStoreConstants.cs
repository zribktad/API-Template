namespace APITemplate.Infrastructure.Idempotency;

internal static class IdempotencyStoreConstants
{
    public const string LockSuffix = ":lock";
    public const string LockValue = "processing";
}
