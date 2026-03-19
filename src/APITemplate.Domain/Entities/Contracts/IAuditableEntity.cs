namespace APITemplate.Domain.Entities.Contracts;

public interface IAuditableEntity
{
    AuditInfo Audit { get; set; }
}
