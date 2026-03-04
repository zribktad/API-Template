namespace APITemplate.Domain.Entities;

public interface IHasRowVersion
{
    byte[] RowVersion { get; set; }
}
