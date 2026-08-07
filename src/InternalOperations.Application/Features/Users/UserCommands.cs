using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Application.Common.Authorization;

namespace InternalOperations.Application.Features.Users;

public sealed record DepartmentSummaryDto(Guid Id, string Name);
public sealed record UserDto(
    Guid Id,
    string UserName,
    string Email,
    string DisplayName,
    bool IsActive,
    DepartmentSummaryDto? Department,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    Guid Version);

public sealed record UserPage(IReadOnlyList<UserDto> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record UserListFilter(
    int Page,
    int PageSize,
    string? Search,
    bool? IsActive,
    Guid? DepartmentId,
    bool? HasDepartment,
    string? Role,
    string SortBy,
    string SortDirection);

public sealed record CreateUserCommand(
    string UserName,
    string Email,
    string DisplayName,
    string InitialPassword,
    IReadOnlyList<string> Roles,
    Guid? DepartmentId) : IRequest<Result<UserDto>>;
public sealed record GetUserQuery(Guid Id) : IRequest<Result<UserDto>>;
public sealed record ListUsersQuery(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    bool? IsActive = null,
    Guid? DepartmentId = null,
    bool? HasDepartment = null,
    string? Role = null,
    string SortBy = "userName",
    string SortDirection = "asc") : IRequest<Result<UserPage>>;
public sealed record UpdateUserCommand(Guid Id, string UserName, string Email, string DisplayName, Guid Version) : IRequest<Result<UserDto>>;
public sealed record SetUserDepartmentCommand(Guid Id, Guid? DepartmentId, Guid Version) : IRequest<Result<UserDto>>;
public sealed record SetUserStatusCommand(Guid Id, bool IsActive, Guid Version) : IRequest<Result<UserDto>>;
public sealed record SetUserRolesCommand(Guid Id, IReadOnlyList<string> Roles, Guid Version) : IRequest<Result<UserDto>>;

public sealed class CreateUserCommandValidator : IRequestValidator<CreateUserCommand>
{
    public Result Validate(CreateUserCommand request) => UserValidation.ValidateCreate(request);
}

public sealed class ListUsersQueryValidator : IRequestValidator<ListUsersQuery>
{
    private static readonly HashSet<string> SortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "userName", "displayName", "email", "createdAtUtc", "updatedAtUtc",
    };

    public Result Validate(ListUsersQuery request)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100
            || !SortFields.Contains(request.SortBy)
            || request.SortDirection is not ("asc" or "desc")
            || (request.DepartmentId.HasValue && request.HasDepartment == false)
            || (request.Role is not null && !ApplicationRoles.All.Contains(request.Role, StringComparer.Ordinal)))
        {
            return Result.Failure(UserErrors.InvalidList);
        }

        return Result.Success();
    }
}

public sealed class UpdateUserCommandValidator : IRequestValidator<UpdateUserCommand>
{
    public Result Validate(UpdateUserCommand request) =>
        request.Id == Guid.Empty || request.Version == Guid.Empty
            ? Result.Failure(UserErrors.InvalidRequest)
            : UserValidation.ValidateProfile(request.UserName, request.Email, request.DisplayName);
}

public sealed class SetUserDepartmentCommandValidator : IRequestValidator<SetUserDepartmentCommand>
{
    public Result Validate(SetUserDepartmentCommand request) =>
        request.Id == Guid.Empty || request.Version == Guid.Empty || request.DepartmentId == Guid.Empty
            ? Result.Failure(UserErrors.InvalidRequest)
            : Result.Success();
}

public sealed class SetUserStatusCommandValidator : IRequestValidator<SetUserStatusCommand>
{
    public Result Validate(SetUserStatusCommand request) =>
        request.Id == Guid.Empty || request.Version == Guid.Empty
            ? Result.Failure(UserErrors.InvalidRequest)
            : Result.Success();
}

public sealed class SetUserRolesCommandValidator : IRequestValidator<SetUserRolesCommand>
{
    public Result Validate(SetUserRolesCommand request) =>
        request.Id == Guid.Empty || request.Version == Guid.Empty
            ? Result.Failure(UserErrors.InvalidRequest)
            : UserValidation.ValidateRoles(request.Roles);
}

public sealed class CreateUserCommandHandler(IUserAdministrationService users)
    : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    public Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken) =>
        users.CreateAsync(request, cancellationToken);
}

public sealed class GetUserQueryHandler(IUserAdministrationService users)
    : IRequestHandler<GetUserQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(request.Id, cancellationToken);
        return user is null ? Result<UserDto>.Failure(UserErrors.NotFound) : Result<UserDto>.Success(user);
    }
}

public sealed class ListUsersQueryHandler(IUserAdministrationService users)
    : IRequestHandler<ListUsersQuery, Result<UserPage>>
{
    public async Task<Result<UserPage>> Handle(ListUsersQuery request, CancellationToken cancellationToken) =>
        Result<UserPage>.Success(await users.ListAsync(new(
            request.Page, request.PageSize, string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            request.IsActive, request.DepartmentId, request.HasDepartment, request.Role, request.SortBy, request.SortDirection), cancellationToken));
}

public sealed class UpdateUserCommandHandler(IUserAdministrationService users)
    : IRequestHandler<UpdateUserCommand, Result<UserDto>>
{
    public Task<Result<UserDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken) => users.UpdateAsync(request, cancellationToken);
}
public sealed class SetUserDepartmentCommandHandler(IUserAdministrationService users)
    : IRequestHandler<SetUserDepartmentCommand, Result<UserDto>>
{
    public Task<Result<UserDto>> Handle(SetUserDepartmentCommand request, CancellationToken cancellationToken) => users.SetDepartmentAsync(request, cancellationToken);
}
public sealed class SetUserStatusCommandHandler(IUserAdministrationService users)
    : IRequestHandler<SetUserStatusCommand, Result<UserDto>>
{
    public Task<Result<UserDto>> Handle(SetUserStatusCommand request, CancellationToken cancellationToken) => users.SetStatusAsync(request, cancellationToken);
}
public sealed class SetUserRolesCommandHandler(IUserAdministrationService users)
    : IRequestHandler<SetUserRolesCommand, Result<UserDto>>
{
    public Task<Result<UserDto>> Handle(SetUserRolesCommand request, CancellationToken cancellationToken) => users.SetRolesAsync(request, cancellationToken);
}

internal static class UserValidation
{
    public static Result ValidateCreate(CreateUserCommand request)
    {
        var profile = ValidateProfile(request.UserName, request.Email, request.DisplayName);
        if (!profile.IsSuccess) return profile;
        if (string.IsNullOrWhiteSpace(request.InitialPassword)) return Result.Failure(UserErrors.InvalidRequest);
        return ValidateRoles(request.Roles);
    }

    public static Result ValidateProfile(string? userName, string? email, string? displayName)
    {
        var normalizedUserName = userName?.Normalize(System.Text.NormalizationForm.FormKC).Trim() ?? string.Empty;
        var normalizedEmail = email?.Normalize(System.Text.NormalizationForm.FormKC).Trim() ?? string.Empty;
        var normalizedDisplayName = displayName?.Normalize(System.Text.NormalizationForm.FormKC).Trim() ?? string.Empty;
        return normalizedUserName.Length is < 1 or > 256
            || normalizedEmail.Length is < 3 or > 256
            || normalizedDisplayName.Length is < 1 or > 200
            ? Result.Failure(UserErrors.InvalidRequest)
            : Result.Success();
    }

    public static Result ValidateRoles(IReadOnlyList<string>? roles)
    {
        if (roles is null || roles.Count == 0
            || roles.Distinct(StringComparer.Ordinal).Count() != roles.Count
            || roles.Any(role => !ApplicationRoles.All.Contains(role, StringComparer.Ordinal)))
        {
            return Result.Failure(UserErrors.InvalidRoles);
        }

        return Result.Success();
    }
}

public static class UserErrors
{
    public static Error InvalidRequest { get; } = Error.Validation("users.invalid_request", "User data is invalid.");
    public static Error PasswordRequirementsNotMet { get; } = Error.Validation("users.password_requirements_not_met", "The password does not meet the configured requirements.");
    public static Error InvalidRoles { get; } = Error.Validation("users.invalid_roles", "Roles must be distinct canonical roles.");
    public static Error InvalidList { get; } = Error.Validation("users.invalid_list", "User list parameters are invalid.");
    public static Error IdentifierConflict { get; } = Error.Conflict("users.identifier_conflict", "A user with this identifier already exists.");
    public static Error VersionConflict { get; } = Error.Conflict("users.version_conflict", "The user was modified by another request.");
    public static Error Inactive { get; } = Error.Conflict("users.inactive", "The user is inactive.");
    public static Error LastAdministrator { get; } = Error.Conflict("users.last_administrator", "The last active administrator cannot be changed.");
    public static Error SelfManagementConflict { get; } = Error.Conflict("users.self_management_conflict", "This change cannot be applied to the current administrator.");
    public static Error NotFound { get; } = Error.NotFound("users.not_found", "User was not found.");
    public static Error DepartmentNotFound { get; } = Error.NotFound("departments.not_found", "Department was not found.");
    public static Error DepartmentInactive { get; } = Error.Conflict("departments.inactive", "The department is inactive.");
}
