using System.ComponentModel.DataAnnotations.Schema;
using InternalOperations.Domain.Common;
using InternalOperations.Domain.Departments;

namespace InternalOperations.Domain.Users;

[Table("Users")]
public class User : AuditableEntity
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
}
