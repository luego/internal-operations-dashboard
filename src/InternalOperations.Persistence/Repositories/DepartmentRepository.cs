using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Application.Features.Departments;
using InternalOperations.Domain.Departments;
using InternalOperations.Domain.Tickets;
using InternalOperations.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Persistence.Repositories;

public sealed class DepartmentRepository(ApplicationDbContext context) : IDepartmentRepository
{
    public Task<Department?> GetTrackedAsync(Guid id, CancellationToken cancellationToken) =>
        context.Departments.SingleOrDefaultAsync(department => department.Id == id, cancellationToken);

    public Task<bool> NormalizedNameExistsAsync(
        string normalizedName,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        context.Departments.AnyAsync(
            department => department.NormalizedName == normalizedName
                && (!excludingId.HasValue || department.Id != excludingId.Value),
            cancellationToken);

    public Task<bool> HasActiveWorkAsync(Guid id, CancellationToken cancellationToken) =>
        context.Tickets.AnyAsync(
            ticket => ticket.DepartmentId == id
                && (ticket.Status == TicketStatus.Open || ticket.Status == TicketStatus.InProgress),
            cancellationToken);

    public async Task AddAsync(Department department, CancellationToken cancellationToken) =>
        await context.Departments.AddAsync(department, cancellationToken);
}

public sealed class DepartmentReadService(ApplicationDbContext context) : IDepartmentReadService
{
    public Task<DepartmentDto?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Departments
            .AsNoTracking()
            .Where(department => department.Id == id)
            .Select(Project())
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResponse<DepartmentDto>> ListAsync(
        DepartmentListFilter filter,
        CancellationToken cancellationToken)
    {
        var query = context.Departments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().Normalize(System.Text.NormalizationForm.FormKC).ToUpperInvariant();
            query = query.Where(department => EF.Functions.Like(department.NormalizedName, $"%{search}%"));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(department => department.IsActive == filter.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var descending = filter.SortDirection == "desc";
        query = filter.SortBy.ToLowerInvariant() switch
        {
            "createdatutc" => descending
                ? query.OrderByDescending(department => department.CreatedAtUtc).ThenBy(department => department.Id)
                : query.OrderBy(department => department.CreatedAtUtc).ThenBy(department => department.Id),
            "updatedatutc" => descending
                ? query.OrderByDescending(department => department.UpdatedAtUtc).ThenBy(department => department.Id)
                : query.OrderBy(department => department.UpdatedAtUtc).ThenBy(department => department.Id),
            _ => descending
                ? query.OrderByDescending(department => department.NormalizedName).ThenBy(department => department.Id)
                : query.OrderBy(department => department.NormalizedName).ThenBy(department => department.Id),
        };

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(Project())
            .ToListAsync(cancellationToken);

        return new PagedResponse<DepartmentDto>(items, filter.Page, filter.PageSize, totalCount);
    }

    private static System.Linq.Expressions.Expression<Func<Department, DepartmentDto>> Project() =>
        department => new DepartmentDto(
            department.Id,
            department.Name,
            department.Description,
            department.IsActive,
            department.CreatedAtUtc,
            department.UpdatedAtUtc,
            department.Version);
}
