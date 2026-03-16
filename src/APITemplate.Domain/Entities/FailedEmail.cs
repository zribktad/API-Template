namespace APITemplate.Domain.Entities;

public sealed class FailedEmail
{
    public Guid Id { get; set; }
    public required string To { get; set; }
    public required string Subject { get; set; }
    public required string HtmlBody { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public string? LastError { get; set; }
    public string? TemplateName { get; set; }
    public bool IsDeadLettered { get; set; }
    public string? ClaimedBy { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
    public DateTime? ClaimedUntilUtc { get; set; }
}
