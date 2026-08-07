using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using InternalOperations.Domain.Common;
using InternalOperations.Domain.Users;

namespace InternalOperations.Domain.Tickets;

[Table("TicketComments")]
public sealed class TicketComment : AuditableEntity
{
    private TicketComment()
    {
    }

    public Guid TicketId { get; private set; }
    public Guid UserId { get; private set; }
    public string Comment { get; private set; } = string.Empty;

    [ForeignKey(nameof(TicketId))]
    public Ticket Ticket { get; private set; } = null!;

    [ForeignKey(nameof(UserId))]
    public User User { get; private set; } = null!;

    public static TicketComment Create(Guid ticketId, Guid userId, string comment, DateTime createdAtUtc)
    {
        if (ticketId == Guid.Empty)
        {
            throw new ArgumentException("Ticket is required.", nameof(ticketId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Author is required.", nameof(userId));
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Created timestamp must be UTC.", nameof(createdAtUtc));
        }

        var canonicalComment = Canonicalize(comment);
        if (canonicalComment.Length is 0 or > 4000)
        {
            throw new ArgumentException("Comment must contain between 1 and 4000 characters.", nameof(comment));
        }

        return new TicketComment
        {
            TicketId = ticketId,
            UserId = userId,
            Comment = canonicalComment,
            CreatedAtUtc = createdAtUtc,
        };
    }

    private static string Canonicalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var pendingSpace = false;
        foreach (var character in value.Normalize(NormalizationForm.FormKC).Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}
