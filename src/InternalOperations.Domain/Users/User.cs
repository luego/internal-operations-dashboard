using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using InternalOperations.Domain.Common;
using InternalOperations.Domain.Departments;

namespace InternalOperations.Domain.Users;

[Table("Users")]
public sealed class User : AuditableEntity
{
    private User()
    {
    }

    public string UserName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public Guid? DepartmentId { get; private set; }
    public Department? Department { get; private set; }
    public Guid Version { get; private set; } = Guid.NewGuid();

    public static User Create(
        Guid id,
        string userName,
        string displayName,
        Guid? departmentId = null,
        DateTime? createdAtUtc = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User identifier is required.", nameof(id));
        }

        var values = Canonicalize(userName, displayName);
        return new User
        {
            Id = id,
            UserName = values.UserName,
            DisplayName = values.DisplayName,
            DepartmentId = departmentId,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
        };
    }

    public void UpdateProfile(string userName, string displayName, DateTime updatedAtUtc)
    {
        var values = Canonicalize(userName, displayName);
        if (UserName == values.UserName && DisplayName == values.DisplayName)
        {
            return;
        }

        UserName = values.UserName;
        DisplayName = values.DisplayName;
        Touch(updatedAtUtc);
    }

    public void AssignDepartment(Guid departmentId, DateTime updatedAtUtc)
    {
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("Department identifier is required.", nameof(departmentId));
        }

        if (DepartmentId == departmentId)
        {
            return;
        }

        DepartmentId = departmentId;
        Touch(updatedAtUtc);
    }

    public void RemoveDepartment(DateTime updatedAtUtc)
    {
        if (DepartmentId is null)
        {
            return;
        }

        DepartmentId = null;
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

    public void RecordAdministrativeChange(DateTime updatedAtUtc) => Touch(updatedAtUtc);

    private static (string UserName, string DisplayName) Canonicalize(string userName, string displayName)
    {
        var canonicalUserName = userName?.Normalize(NormalizationForm.FormKC).Trim() ?? string.Empty;
        var canonicalDisplayName = CollapseWhitespace(displayName ?? string.Empty);
        if (canonicalUserName.Length is < 1 or > 256)
        {
            throw new ArgumentException("Username must contain between 1 and 256 characters.", nameof(userName));
        }

        if (canonicalDisplayName.Length is < 1 or > 200)
        {
            throw new ArgumentException("Display name must contain between 1 and 200 characters.", nameof(displayName));
        }

        return (canonicalUserName, canonicalDisplayName);
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
