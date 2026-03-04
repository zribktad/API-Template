namespace APITemplate.Domain.Entities;

public sealed class AuditInfo
{
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = AuditDefaults.SystemActorId;
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = AuditDefaults.SystemActorId;
}
