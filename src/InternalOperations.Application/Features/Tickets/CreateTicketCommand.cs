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

public sealed class CreateTicketCommandValidator : IRequestValidator<CreateTicketCommand>
{
    public Result Validate(CreateTicketCommand request)
    {
        var title = request.Title?.Normalize(System.Text.NormalizationForm.FormKC).Trim() ?? string.Empty;
        var description = request.Description?.Normalize(System.Text.NormalizationForm.FormKC).Trim() ?? string.Empty;
        return title.Length is < 1 or > 200
            || description.Length is < 1 or > 4000
            || request.DepartmentId == Guid.Empty
            || request.UserId == Guid.Empty
            || !Enum.IsDefined(request.Priority)
                ? Result.Failure(TicketErrors.InvalidRequest)
                : Result.Success();
    }
}

public static class TicketErrors
{
    public static Error InvalidRequest { get; } = Error.Validation("tickets.invalid_request", "Ticket data is invalid.");
    public static Error NotFound { get; } = Error.NotFound("tickets.not_found", "Ticket was not found.");
    public static Error DepartmentNotFound { get; } = Error.NotFound("departments.not_found", "Department was not found.");
    public static Error DepartmentInactive { get; } = Error.Conflict("departments.inactive", "The department is inactive.");
    public static Error UserNotFound { get; } = Error.NotFound("users.not_found", "User was not found.");
    public static Error UserInactive { get; } = Error.Conflict("users.inactive", "The user is inactive.");
    public static Error VersionConflict { get; } = Error.Conflict("tickets.version_conflict", "The ticket was modified by another request.");
    public static Error InvalidTransition { get; } = Error.Conflict("tickets.invalid_transition", "The requested ticket status transition is not allowed.");
}
