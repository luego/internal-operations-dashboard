using System.ComponentModel.DataAnnotations.Schema;
using InternalOperations.Domain.Common;

namespace InternalOperations.Domain.Departments;

[Table("Departments")]
public sealed class Department : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive {get; set; } = true;
}