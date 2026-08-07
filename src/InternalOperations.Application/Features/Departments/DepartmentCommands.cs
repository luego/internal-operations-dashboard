using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Domain.Departments;

namespace InternalOperations.Application.Features.Departments;

public sealed record DepartmentDto(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    Guid Version)
{
    public static DepartmentDto From(Department department) => new(
        department.Id,
        department.Name,
        department.Description,
        department.IsActive,
        department.CreatedAtUtc,
        department.UpdatedAtUtc,
        department.Version);
}

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record DepartmentListFilter(
    int Page,
    int PageSize,
    string? Search,
    bool? IsActive,
    string SortBy,
    string SortDirection);

public sealed record CreateDepartmentCommand(string Name, string? Description) : IRequest<Result<DepartmentDto>>;
public sealed record GetDepartmentQuery(Guid Id) : IRequest<Result<DepartmentDto>>;
public sealed record ListDepartmentsQuery(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    bool? IsActive = null,
    string SortBy = "name",
    string SortDirection = "asc") : IRequest<Result<PagedResponse<DepartmentDto>>>;
public sealed record UpdateDepartmentCommand(
    Guid Id,
    string Name,
    string? Description,
    Guid Version) : IRequest<Result<DepartmentDto>>;
public sealed record SetDepartmentStatusCommand(
    Guid Id,
    bool IsActive,
    Guid Version) : IRequest<Result<DepartmentDto>>;

public sealed class CreateDepartmentCommandValidator : IRequestValidator<CreateDepartmentCommand>
{
    public Result Validate(CreateDepartmentCommand request) =>
        DepartmentValidation.ValidateValues(request.Name, request.Description);
}

public sealed class ListDepartmentsQueryValidator : IRequestValidator<ListDepartmentsQuery>
{
    private static readonly HashSet<string> SortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "name",
        "createdAtUtc",
        "updatedAtUtc",
    };

    public Result Validate(ListDepartmentsQuery request) =>
        request.Page < 1
        || request.PageSize is < 1 or > 100
        || !SortFields.Contains(request.SortBy)
        || request.SortDirection is not ("asc" or "desc")
            ? Result.Failure(DepartmentErrors.InvalidList)
            : Result.Success();
}

public sealed class UpdateDepartmentCommandValidator : IRequestValidator<UpdateDepartmentCommand>
{
    public Result Validate(UpdateDepartmentCommand request) =>
        request.Id == Guid.Empty || request.Version == Guid.Empty
            ? Result.Failure(DepartmentErrors.InvalidRequest)
            : DepartmentValidation.ValidateValues(request.Name, request.Description);
}

public sealed class SetDepartmentStatusCommandValidator : IRequestValidator<SetDepartmentStatusCommand>
{
    public Result Validate(SetDepartmentStatusCommand request) =>
        request.Id == Guid.Empty || request.Version == Guid.Empty
            ? Result.Failure(DepartmentErrors.InvalidRequest)
            : Result.Success();
}

public sealed class CreateDepartmentCommandHandler(
    IDepartmentRepository departments,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<CreateDepartmentCommand, Result<DepartmentDto>>
{
    public async Task<Result<DepartmentDto>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        Department department;
        try
        {
            department = Department.Create(request.Name, request.Description, clock.UtcNow.UtcDateTime);
        }
        catch (ArgumentException)
        {
            return Result<DepartmentDto>.Failure(DepartmentErrors.InvalidRequest);
        }

        if (await departments.NormalizedNameExistsAsync(department.NormalizedName, null, cancellationToken))
        {
            return Result<DepartmentDto>.Failure(DepartmentErrors.NameConflict);
        }

        await departments.AddAsync(department, cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceUniqueConstraintException)
        {
            return Result<DepartmentDto>.Failure(DepartmentErrors.NameConflict);
        }

        return Result<DepartmentDto>.Success(DepartmentDto.From(department));
    }
}

public sealed class GetDepartmentQueryHandler(IDepartmentReadService departments)
    : IRequestHandler<GetDepartmentQuery, Result<DepartmentDto>>
{
    public async Task<Result<DepartmentDto>> Handle(GetDepartmentQuery request, CancellationToken cancellationToken)
    {
        var department = await departments.GetAsync(request.Id, cancellationToken);
        return department is null
            ? Result<DepartmentDto>.Failure(DepartmentErrors.NotFound)
            : Result<DepartmentDto>.Success(department);
    }
}

public sealed class ListDepartmentsQueryHandler(IDepartmentReadService departments)
    : IRequestHandler<ListDepartmentsQuery, Result<PagedResponse<DepartmentDto>>>
{
    public async Task<Result<PagedResponse<DepartmentDto>>> Handle(
        ListDepartmentsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new DepartmentListFilter(
            request.Page,
            request.PageSize,
            string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            request.IsActive,
            request.SortBy,
            request.SortDirection);
        var page = await departments.ListAsync(filter, cancellationToken);
        return Result<PagedResponse<DepartmentDto>>.Success(page);
    }
}

public sealed class UpdateDepartmentCommandHandler(
    IDepartmentRepository departments,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<UpdateDepartmentCommand, Result<DepartmentDto>>
{
    public async Task<Result<DepartmentDto>> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await departments.GetTrackedAsync(request.Id, cancellationToken);
        if (department is null)
        {
            return Result<DepartmentDto>.Failure(DepartmentErrors.NotFound);
        }

        if (department.Version != request.Version)
        {
            return Result<DepartmentDto>.Failure(DepartmentErrors.VersionConflict);
        }

        Department candidate;
        try
        {
            candidate = Department.Create(request.Name, request.Description);
        }
        catch (ArgumentException)
        {
            return Result<DepartmentDto>.Failure(DepartmentErrors.InvalidRequest);
        }

        if (await departments.NormalizedNameExistsAsync(candidate.NormalizedName, department.Id, cancellationToken))
        {
            return Result<DepartmentDto>.Failure(DepartmentErrors.NameConflict);
        }

        var previousVersion = department.Version;
        department.Update(request.Name, request.Description, clock.UtcNow.UtcDateTime);
        if (department.Version == previousVersion)
        {
            return Result<DepartmentDto>.Success(DepartmentDto.From(department));
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceUniqueConstraintException)
        {
            return Result<DepartmentDto>.Failure(DepartmentErrors.NameConflict);
        }
        catch (PersistenceConcurrencyException)
        {
            return Result<DepartmentDto>.Failure(DepartmentErrors.VersionConflict);
        }

        return Result<DepartmentDto>.Success(DepartmentDto.From(department));
    }
}

public sealed class SetDepartmentStatusCommandHandler(
    IDepartmentRepository departments,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<SetDepartmentStatusCommand, Result<DepartmentDto>>
{
    public async Task<Result<DepartmentDto>> Handle(
        SetDepartmentStatusCommand request,
        CancellationToken cancellationToken)
    {
        var department = await departments.GetTrackedAsync(request.Id, cancellationToken);
        if (department is null)
        {
            return Result<DepartmentDto>.Failure(DepartmentErrors.NotFound);
        }

        if (department.Version != request.Version)
        {
            return Result<DepartmentDto>.Failure(DepartmentErrors.VersionConflict);
        }

        if (department.IsActive == request.IsActive)
        {
            return Result<DepartmentDto>.Success(DepartmentDto.From(department));
        }

        if (!request.IsActive && await departments.HasActiveWorkAsync(department.Id, cancellationToken))
        {
            return Result<DepartmentDto>.Failure(DepartmentErrors.ActiveWorkConflict);
        }

        if (request.IsActive)
        {
            department.Activate(clock.UtcNow.UtcDateTime);
        }
        else
        {
            department.Deactivate(clock.UtcNow.UtcDateTime);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConcurrencyException)
        {
            return Result<DepartmentDto>.Failure(DepartmentErrors.VersionConflict);
        }

        return Result<DepartmentDto>.Success(DepartmentDto.From(department));
    }
}

internal static class DepartmentValidation
{
    public static Result ValidateValues(string? name, string? description)
    {
        var normalizedName = name?.Normalize(System.Text.NormalizationForm.FormKC).Trim() ?? string.Empty;
        if (normalizedName.Length is < 1 or > 100)
        {
            return Result.Failure(DepartmentErrors.InvalidName);
        }

        var normalizedDescription = description?.Normalize(System.Text.NormalizationForm.FormKC).Trim() ?? string.Empty;
        return normalizedDescription.Length > 500
            ? Result.Failure(DepartmentErrors.InvalidDescription)
            : Result.Success();
    }
}

public static class DepartmentErrors
{
    public static Error InvalidName { get; } = Error.Validation("departments.invalid_name", "Department name must contain between 1 and 100 characters.");
    public static Error InvalidDescription { get; } = Error.Validation("departments.invalid_description", "Department description cannot exceed 500 characters.");
    public static Error InvalidRequest { get; } = Error.Validation("departments.invalid_request", "Department data is invalid.");
    public static Error InvalidList { get; } = Error.Validation("departments.invalid_list", "Department list parameters are invalid.");
    public static Error NameConflict { get; } = Error.Conflict("departments.name_conflict", "A department with this name already exists.");
    public static Error VersionConflict { get; } = Error.Conflict("departments.version_conflict", "The department was modified by another request.");
    public static Error ActiveWorkConflict { get; } = Error.Conflict("departments.active_work_conflict", "A department with active work cannot be deactivated.");
    public static Error NotFound { get; } = Error.NotFound("departments.not_found", "Department was not found.");
}
