using InternalOperations.Domain.Common;

namespace InternalOperations.Domain.Entities;

public sealed class Ticket : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
