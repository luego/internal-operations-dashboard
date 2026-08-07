using System.ComponentModel.DataAnnotations;

namespace InternalOperations.Domain.Common;

public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public bool IsDeleted { get; protected set; }

    public void Delete() => IsDeleted = true;
    public void Restore() => IsDeleted = false;
}
