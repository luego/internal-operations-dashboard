using InternalOperations.Domain.Tickets;

namespace InternalOperations.Application.Features.TicketCollaboration;

public sealed record TicketCommentDto(
    Guid Id,
    Guid TicketId,
    Guid AuthorId,
    string AuthorDisplayName,
    string Comment,
    DateTime CreatedAtUtc);

public sealed record TicketActivityDto(
    Guid Id,
    Guid TicketId,
    Guid? ActorUserId,
    string? ActorDisplayName,
    TicketActivityType Type,
    string Description,
    DateTime OccurredAtUtc);

public sealed record TicketCommentPage(
    IReadOnlyList<TicketCommentDto> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record TicketHistoryPage(
    IReadOnlyList<TicketActivityDto> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record AddTicketCommentCommand(Guid TicketId, string Comment)
    : IRequest<Result<TicketCommentDto>>;

public sealed record ListTicketCommentsQuery(Guid TicketId, int Page = 1, int PageSize = 25)
    : IRequest<Result<TicketCommentPage>>;

public sealed record GetTicketHistoryQuery(Guid TicketId, int Page = 1, int PageSize = 50)
    : IRequest<Result<TicketHistoryPage>>;

public sealed class AddTicketCommentCommandValidator : IRequestValidator<AddTicketCommentCommand>
{
    public Result Validate(AddTicketCommentCommand request)
    {
        var comment = request.Comment?.Trim();
        return request.TicketId == Guid.Empty || string.IsNullOrWhiteSpace(comment) || comment.Length > 4000
            ? Result.Failure(TicketCollaborationErrors.InvalidRequest)
            : Result.Success();
    }
}

public sealed class ListTicketCommentsQueryValidator : IRequestValidator<ListTicketCommentsQuery>
{
    public Result Validate(ListTicketCommentsQuery request) =>
        request.TicketId == Guid.Empty || request.Page < 1 || request.PageSize is < 1 or > 100
            ? Result.Failure(TicketCollaborationErrors.InvalidPagination)
            : Result.Success();
}

public sealed class GetTicketHistoryQueryValidator : IRequestValidator<GetTicketHistoryQuery>
{
    public Result Validate(GetTicketHistoryQuery request) =>
        request.TicketId == Guid.Empty || request.Page < 1 || request.PageSize is < 1 or > 100
            ? Result.Failure(TicketCollaborationErrors.InvalidPagination)
            : Result.Success();
}

public static class TicketCollaborationErrors
{
    public static Error InvalidRequest { get; } = Error.Validation("comments.invalid_request", "Comment data is invalid.");
    public static Error InvalidPagination { get; } = Error.Validation("comments.invalid_pagination", "Pagination is invalid.");
    public static Error AuthorRequired { get; } = Error.Unauthorized("comments.author_required", "An authenticated author is required.");
    public static Error AuthorNotFound { get; } = Error.NotFound("users.not_found", "User was not found.");
}
