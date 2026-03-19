namespace APITemplate.Api.Filters.Idempotency;

[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentAttribute : Attribute
{
    public int TtlHours { get; set; } = IdempotencyConstants.DefaultTtlHours;
    public int LockTimeoutSeconds { get; set; } = IdempotencyConstants.LockTimeoutSeconds;
}
