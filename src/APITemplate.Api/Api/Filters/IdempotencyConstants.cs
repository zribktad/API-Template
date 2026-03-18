namespace APITemplate.Api.Filters;

public static class IdempotencyConstants
{
    public const string HeaderName = "Idempotency-Key";
    public const int DefaultTtlHours = 24;
    public const int MaxKeyLength = 100;
}
