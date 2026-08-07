using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using InternalOperations.Domain.Common;
using InternalOperations.Domain.Tickets;
using InternalOperations.Domain.Users;

namespace InternalOperations.Domain.Departments;

[Table("Departments")]
public sealed class Department : AuditableEntity
{
    private Department()
    {
    }

    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public Guid Version { get; private set; } = Guid.NewGuid();
    public ICollection<User> Users { get; } = new List<User>();
    public ICollection<Ticket> Tickets { get; } = new List<Ticket>();

    public static Department Create(string name, string? description, DateTime? createdAtUtc = null)
    {
        var values = Canonicalize(name, description);
        return new Department
        {
            Name = values.Name,
            NormalizedName = values.NormalizedName,
            Description = values.Description,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
        };
    }

    public void Update(string name, string? description, DateTime updatedAtUtc)
    {
        var values = Canonicalize(name, description);
        if (Name == values.Name && Description == values.Description)
        {
            return;
        }

        Name = values.Name;
        NormalizedName = values.NormalizedName;
        Description = values.Description;
        Touch(updatedAtUtc);
    }

    public void Activate(DateTime updatedAtUtc)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Touch(updatedAtUtc);
    }

    public void Deactivate(DateTime updatedAtUtc)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Touch(updatedAtUtc);
    }

    private static (string Name, string NormalizedName, string Description) Canonicalize(
        string name,
        string? description)
    {
        var canonicalName = CollapseWhitespace(name);
        var canonicalDescription = CollapseWhitespace(description ?? string.Empty);

        if (canonicalName.Length is < 1 or > 100)
        {
            throw new ArgumentException("Department name must contain between 1 and 100 characters.", nameof(name));
        }

        if (canonicalDescription.Length > 500)
        {
            throw new ArgumentException("Department description cannot exceed 500 characters.", nameof(description));
        }

        return (canonicalName, canonicalName.ToUpperInvariant(), canonicalDescription);
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
