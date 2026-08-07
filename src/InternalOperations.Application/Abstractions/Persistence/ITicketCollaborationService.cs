using InternalOperations.Application.Features.TicketCollaboration;

namespace InternalOperations.Application.Abstractions.Persistence;

public interface ITicketCollaborationService
{
    Task<Result<TicketCommentDto>> AddCommentAsync(
        Guid ticketId,
        Guid authorId,
        string comment,
        CancellationToken cancellationToken);

    Task<Result<TicketCommentPage>> ListCommentsAsync(
        Guid ticketId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<Result<TicketHistoryPage>> GetHistoryAsync(
        Guid ticketId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
