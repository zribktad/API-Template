namespace APITemplate.Domain.Entities;

public sealed class PasswordResetToken : IAuditableTenantEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; }

    public AppUser User { get; set; } = null!;

    public Guid TenantId { get; set; }
    public AuditInfo Audit { get; set; } = new();
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }
}
