using InternalOperations.Domain.Tickets;

namespace InternalOperations.Application.Features.Tickets;

public sealed record TicketDepartmentDto(Guid Id, string Name);
public sealed record TicketAssigneeDto(Guid Id, string DisplayName);
public sealed record TicketDto(
    Guid Id,
    int Number,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    TicketDepartmentDto Department,
    TicketAssigneeDto? Assignee,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    Guid Version);

public sealed record CreateTicketCommand(
    string Title,
    string Description,
    TicketPriority Priority,
    Guid DepartmentId,
    Guid? UserId) : IRequest<Result<TicketDto>>;

public sealed record GetTicketQuery(Guid Id) : IRequest<Result<TicketDto>>;

public sealed record TicketListFilter(
    int Page,
    int PageSize,
    string? Search,
    TicketStatus? Status,
    TicketPriority? Priority,
    Guid? DepartmentId,
    Guid? UserId,
    string SortBy = "createdAtUtc",
    string SortDirection = "desc");

public sealed record TicketPage(IReadOnlyList<TicketDto> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record ListTicketsQuery(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    TicketStatus? Status = null,
    TicketPriority? Priority = null,
    Guid? DepartmentId = null,
    Guid? UserId = null,
    string SortBy = "createdAtUtc",
    string SortDirection = "desc") : IRequest<Result<TicketPage>>;

public sealed record UpdateTicketCommand(
    Guid Id,
    string Title,
    string Description,
    TicketPriority Priority,
    Guid DepartmentId,
    Guid? UserId,
    Guid Version) : IRequest<Result<TicketDto>>;

public sealed record ChangeTicketStatusCommand(Guid Id, TicketStatus Status, Guid Version)
    : IRequest<Result<TicketDto>>;

public sealed class CreateTicketCommandValidator : IRequestValidator<CreateTicketCommand>
{
    public Result Validate(CreateTicketCommand request) => TicketValidation.ValidateValues(
        request.Title,
        request.Description,
        request.Priority,
        request.DepartmentId,
        request.UserId);
}

public sealed class ListTicketsQueryValidator : IRequestValidator<ListTicketsQuery>
{
    private static readonly HashSet<string> SortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "number",
        "createdAtUtc",
        "updatedAtUtc",
        "priority",
        "status",
    };

    public Result Validate(ListTicketsQuery request) =>
        request.Page < 1
        || request.PageSize is < 1 or > 100
        || request.DepartmentId == Guid.Empty
        || request.UserId == Guid.Empty
        || !SortFields.Contains(request.SortBy)
        || request.SortDirection is not ("asc" or "desc")
        || (request.Status.HasValue && !Enum.IsDefined(request.Status.Value))
        || (request.Priority.HasValue && !Enum.IsDefined(request.Priority.Value))
            ? Result.Failure(TicketErrors.InvalidList)
            : Result.Success();
}

public sealed class UpdateTicketCommandValidator : IRequestValidator<UpdateTicketCommand>
{
    public Result Validate(UpdateTicketCommand request) =>
        request.Id == Guid.Empty || request.Version == Guid.Empty
            ? Result.Failure(TicketErrors.InvalidRequest)
            : TicketValidation.ValidateValues(
                request.Title,
                request.Description,
                request.Priority,
                request.DepartmentId,
                request.UserId);
}

public sealed class ChangeTicketStatusCommandValidator : IRequestValidator<ChangeTicketStatusCommand>
{
    public Result Validate(ChangeTicketStatusCommand request) =>
        request.Id == Guid.Empty || request.Version == Guid.Empty || !Enum.IsDefined(request.Status)
            ? Result.Failure(TicketErrors.InvalidRequest)
            : Result.Success();
}

internal static class TicketValidation
{
    public static Result ValidateValues(
        string? titleValue,
        string? descriptionValue,
        TicketPriority priority,
        Guid departmentId,
        Guid? userId)
    {
        var title = titleValue?.Normalize(System.Text.NormalizationForm.FormKC).Trim() ?? string.Empty;
        var description = descriptionValue?.Normalize(System.Text.NormalizationForm.FormKC).Trim() ?? string.Empty;
        return title.Length is < 1 or > 200
            || description.Length is < 1 or > 4000
            || departmentId == Guid.Empty
            || userId == Guid.Empty
            || !Enum.IsDefined(priority)
                ? Result.Failure(TicketErrors.InvalidRequest)
                : Result.Success();
    }
}

public static class TicketErrors
{
    public static Error InvalidRequest { get; } = Error.Validation("tickets.invalid_request", "Ticket data is invalid.");
    public static Error InvalidList { get; } = Error.Validation("tickets.invalid_list", "Ticket list parameters are invalid.");
    public static Error NotFound { get; } = Error.NotFound("tickets.not_found", "Ticket was not found.");
    public static Error DepartmentNotFound { get; } = Error.NotFound("departments.not_found", "Department was not found.");
    public static Error DepartmentInactive { get; } = Error.Conflict("departments.inactive", "The department is inactive.");
    public static Error UserNotFound { get; } = Error.NotFound("users.not_found", "User was not found.");
    public static Error UserInactive { get; } = Error.Conflict("users.inactive", "The user is inactive.");
    public static Error VersionConflict { get; } = Error.Conflict("tickets.version_conflict", "The ticket was modified by another request.");
    public static Error InvalidTransition { get; } = Error.Conflict("tickets.invalid_transition", "The requested ticket status transition is not allowed.");
}
