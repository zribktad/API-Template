namespace APITemplate.Domain.Entities.Contracts;

public interface IAuditableTenantEntity : ITenantEntity, IAuditableEntity, ISoftDeletable
{
}
