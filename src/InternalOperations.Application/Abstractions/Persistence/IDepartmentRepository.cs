using InternalOperations.Application.Features.Departments;
using InternalOperations.Domain.Departments;

namespace InternalOperations.Application.Abstractions.Persistence;

public interface IDepartmentRepository
{
    Task<Department?> GetTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> NormalizedNameExistsAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> HasActiveWorkAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Department department, CancellationToken cancellationToken);
}

public interface IDepartmentReadService
{
    Task<DepartmentDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<DepartmentDto>> ListAsync(DepartmentListFilter filter, CancellationToken cancellationToken);
}
