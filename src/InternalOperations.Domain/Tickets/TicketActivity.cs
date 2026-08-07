using System.Text;

namespace InternalOperations.Domain.Tickets;

public enum TicketActivityType
{
    Created = 1,
    Updated = 2,
    StatusChanged = 3,
    CommentAdded = 4,
}

public sealed class TicketActivity
{
    private TicketActivity()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TicketId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public TicketActivityType Type { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }
    public Ticket Ticket { get; private set; } = null!;

    public static TicketActivity Create(
        Guid ticketId,
        Guid? actorUserId,
        TicketActivityType type,
        string description,
        DateTime occurredAtUtc)
    {
        if (ticketId == Guid.Empty)
        {
            throw new ArgumentException("Ticket is required.", nameof(ticketId));
        }

        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException("Actor cannot be empty.", nameof(actorUserId));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentException("Activity type is invalid.", nameof(type));
        }

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Activity timestamp must be UTC.", nameof(occurredAtUtc));
        }

        var canonicalDescription = Canonicalize(description);
        if (canonicalDescription.Length is 0 or > 500)
        {
            throw new ArgumentException("Description must contain between 1 and 500 characters.", nameof(description));
        }

        return new TicketActivity
        {
            TicketId = ticketId,
            ActorUserId = actorUserId,
            Type = type,
            Description = canonicalDescription,
            OccurredAtUtc = occurredAtUtc,
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
