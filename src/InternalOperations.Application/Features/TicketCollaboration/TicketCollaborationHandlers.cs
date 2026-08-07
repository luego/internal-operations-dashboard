using InternalOperations.Application.Abstractions.Persistence;

namespace InternalOperations.Application.Features.TicketCollaboration;

public sealed class AddTicketCommentCommandHandler(
    ITicketCollaborationService collaboration,
    ICurrentUser currentUser)
    : IRequestHandler<AddTicketCommentCommand, Result<TicketCommentDto>>
{
    public Task<Result<TicketCommentDto>> Handle(
        AddTicketCommentCommand request,
        CancellationToken cancellationToken) =>
        currentUser.UserId is { } authorId
            ? collaboration.AddCommentAsync(request.TicketId, authorId, request.Comment, cancellationToken)
            : Task.FromResult(Result<TicketCommentDto>.Failure(TicketCollaborationErrors.AuthorRequired));
}

public sealed class ListTicketCommentsQueryHandler(ITicketCollaborationService collaboration)
    : IRequestHandler<ListTicketCommentsQuery, Result<TicketCommentPage>>
{
    public Task<Result<TicketCommentPage>> Handle(
        ListTicketCommentsQuery request,
        CancellationToken cancellationToken) =>
        collaboration.ListCommentsAsync(request.TicketId, request.Page, request.PageSize, cancellationToken);
}

public sealed class GetTicketHistoryQueryHandler(ITicketCollaborationService collaboration)
    : IRequestHandler<GetTicketHistoryQuery, Result<TicketHistoryPage>>
{
    public Task<Result<TicketHistoryPage>> Handle(
        GetTicketHistoryQuery request,
        CancellationToken cancellationToken) =>
        collaboration.GetHistoryAsync(request.TicketId, request.Page, request.PageSize, cancellationToken);
}
