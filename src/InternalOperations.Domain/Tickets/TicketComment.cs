using System.ComponentModel.DataAnnotations.Schema;
using InternalOperations.Domain.Common;
using InternalOperations.Domain.Users;

namespace InternalOperations.Domain.Tickets;

[Table("TicketComments")]
public class TicketComment : AuditableEntity
{
    public Guid TicketId { get; set; }
    public Guid UserId { get; set; }
    public string Comment { get; set; } = string.Empty;
    [ForeignKey(nameof(TicketId))]
    public Ticket Ticket { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}