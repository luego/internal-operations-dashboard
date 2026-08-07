using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using InternalOperations.Domain.Common;
using InternalOperations.Domain.Departments;
using InternalOperations.Domain.Users;

namespace InternalOperations.Domain.Tickets;

[Table("Tickets")]
public sealed class Ticket : AuditableEntity
{
    private Ticket()
    {
    }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Number { get; private set; }
    public TicketStatus Status { get; private set; } = TicketStatus.Open;
    public TicketPriority Priority { get; private set; } = TicketPriority.Medium;
    public Guid? UserId { get; private set; }
    public User? User { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Department? Department { get; private set; }
    public Guid Version { get; private set; } = Guid.NewGuid();

    public static Ticket Create(
        string title,
        string description,
        TicketPriority priority,
        Guid departmentId,
        Guid? userId,
        DateTime? createdAtUtc = null)
    {
        var values = Canonicalize(title, description, departmentId, userId);
        return new Ticket
        {
            Title = values.Title,
            Description = values.Description,
            Priority = priority,
            DepartmentId = departmentId,
            UserId = userId,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
        };
    }

    public void UpdateDetails(
        string title,
        string description,
        TicketPriority priority,
        Guid departmentId,
        Guid? userId,
        DateTime updatedAtUtc)
    {
        var values = Canonicalize(title, description, departmentId, userId);
        if (Title == values.Title && Description == values.Description && Priority == priority
            && DepartmentId == departmentId && UserId == userId)
        {
            return;
        }

        Title = values.Title;
        Description = values.Description;
        Priority = priority;
        DepartmentId = departmentId;
        UserId = userId;
        Touch(updatedAtUtc);
    }

    public bool TryTransitionTo(TicketStatus status, DateTime updatedAtUtc)
    {
        if (Status == status)
        {
            return true;
        }

        var allowed = Status switch
        {
            TicketStatus.Open => status is TicketStatus.InProgress or TicketStatus.Closed,
            TicketStatus.InProgress => status is TicketStatus.Resolved or TicketStatus.Closed,
            TicketStatus.Resolved => status is TicketStatus.InProgress or TicketStatus.Closed,
            _ => false,
        };
        if (!allowed)
        {
            return false;
        }

        Status = status;
        Touch(updatedAtUtc);
        return true;
    }

    private static (string Title, string Description) Canonicalize(
        string title,
        string description,
        Guid departmentId,
        Guid? userId)
    {
        var canonicalTitle = CollapseWhitespace(title ?? string.Empty);
        var canonicalDescription = CollapseWhitespace(description ?? string.Empty);
        if (canonicalTitle.Length is < 1 or > 200)
        {
            throw new ArgumentException("Ticket title must contain between 1 and 200 characters.", nameof(title));
        }

        if (canonicalDescription.Length is < 1 or > 4000)
        {
            throw new ArgumentException("Ticket description must contain between 1 and 4000 characters.", nameof(description));
        }

        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("Department identifier is required.", nameof(departmentId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        return (canonicalTitle, canonicalDescription);
    }

    private static string CollapseWhitespace(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormKC);
        var result = new StringBuilder(normalized.Length);
        var pendingSpace = false;
        foreach (var character in normalized)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = result.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                result.Append(' ');
                pendingSpace = false;
            }

            result.Append(character);
        }

        return result.ToString();
    }

    private void Touch(DateTime updatedAtUtc)
    {
        UpdatedAtUtc = updatedAtUtc;
        Version = Guid.NewGuid();
    }
}
