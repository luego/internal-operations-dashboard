using System.ComponentModel.DataAnnotations.Schema;
using InternalOperations.Domain.Common;
using InternalOperations.Domain.Departments;
using InternalOperations.Domain.Users;

namespace InternalOperations.Domain.Tickets;

[Table("Tickets")]
public sealed class Ticket : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Number { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public Guid? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public Guid? DepartmentId { get; set; }
    [ForeignKey(nameof(DepartmentId))]
    public Department Department { get; set; } = null!;
}