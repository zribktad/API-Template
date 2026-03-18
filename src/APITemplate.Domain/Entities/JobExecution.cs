using APITemplate.Domain.Enums;

namespace APITemplate.Domain.Entities;

public sealed class JobExecution : IAuditableTenantEntity
{
    public Guid Id { get; set; }
    public required string JobType { get; init; }
    public JobStatus Status { get; private set; } = JobStatus.Pending;
    public int ProgressPercent { get; private set; }
    public string? Parameters { get; init; }
    public string? ResultPayload { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime SubmittedAtUtc { get; init; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public Guid TenantId { get; set; }
    public AuditInfo Audit { get; set; } = new();
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }

    public void MarkProcessing(TimeProvider timeProvider)
    {
        Status = JobStatus.Processing;
        StartedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
    }

    public void MarkCompleted(string? resultPayload, TimeProvider timeProvider)
    {
        Status = JobStatus.Completed;
        ProgressPercent = 100;
        ResultPayload = resultPayload;
        CompletedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
    }

    public void MarkFailed(string errorMessage, TimeProvider timeProvider)
    {
        Status = JobStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
    }

    public void UpdateProgress(int percent)
    {
        ProgressPercent = Math.Clamp(percent, 0, 100);
    }
}
