using InternalOperations.Application;
using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Application.Features.TicketCollaboration;
using InternalOperations.Application.Features.Tickets;
using InternalOperations.Domain.Tickets;
using InternalOperations.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Persistence.Repositories;

public sealed class TicketCollaborationService(ApplicationDbContext context, IClock clock)
    : ITicketCollaborationService
{
    public async Task<Result<TicketCommentDto>> AddCommentAsync(
        Guid ticketId,
        Guid authorId,
        string comment,
        CancellationToken cancellationToken)
    {
        if (!await context.Tickets.AnyAsync(ticket => ticket.Id == ticketId, cancellationToken))
        {
            return Result<TicketCommentDto>.Failure(TicketErrors.NotFound);
        }

        var author = await context.DomainUsers
            .AsNoTracking()
            .Where(user => user.Id == authorId && user.IsActive)
            .Select(user => new { user.Id, user.DisplayName })
            .SingleOrDefaultAsync(cancellationToken);
        if (author is null)
        {
            return Result<TicketCommentDto>.Failure(TicketCollaborationErrors.AuthorNotFound);
        }

        var now = clock.UtcNow.UtcDateTime;
        var entity = TicketComment.Create(ticketId, authorId, comment, now);
        var activity = TicketActivity.Create(
            ticketId,
            authorId,
            TicketActivityType.CommentAdded,
            "Comment added",
            now);
        context.TicketComments.Add(entity);
        context.TicketActivities.Add(activity);
        await context.SaveChangesAsync(cancellationToken);

        return Result<TicketCommentDto>.Success(new TicketCommentDto(
            entity.Id,
            ticketId,
            author.Id,
            author.DisplayName,
            entity.Comment,
            entity.CreatedAtUtc));
    }

    public async Task<Result<TicketCommentPage>> ListCommentsAsync(
        Guid ticketId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!await context.Tickets.AsNoTracking().AnyAsync(ticket => ticket.Id == ticketId, cancellationToken))
        {
            return Result<TicketCommentPage>.Failure(TicketErrors.NotFound);
        }

        var query = context.TicketComments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(comment => !comment.IsDeleted && comment.TicketId == ticketId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(comment => comment.CreatedAtUtc)
            .ThenBy(comment => comment.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(comment => new TicketCommentDto(
                comment.Id,
                comment.TicketId,
                comment.UserId,
                comment.User.DisplayName,
                comment.Comment,
                comment.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result<TicketCommentPage>.Success(new TicketCommentPage(items, page, pageSize, totalCount));
    }

    public async Task<Result<TicketHistoryPage>> GetHistoryAsync(
        Guid ticketId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!await context.Tickets.AsNoTracking().AnyAsync(ticket => ticket.Id == ticketId, cancellationToken))
        {
            return Result<TicketHistoryPage>.Failure(TicketErrors.NotFound);
        }

        var query = context.TicketActivities.AsNoTracking().Where(activity => activity.TicketId == ticketId);
        var totalCount = await query.CountAsync(cancellationToken);
        var activities = await query
            .OrderBy(activity => activity.OccurredAtUtc)
            .ThenBy(activity => activity.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var actorIds = activities
            .Where(activity => activity.ActorUserId.HasValue)
            .Select(activity => activity.ActorUserId!.Value)
            .Distinct()
            .ToArray();
        var actors = await context.DomainUsers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(user => actorIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);
        var items = activities.Select(activity => new TicketActivityDto(
            activity.Id,
            activity.TicketId,
            activity.ActorUserId,
            activity.ActorUserId is { } actorId && actors.TryGetValue(actorId, out var name) ? name : null,
            activity.Type,
            activity.Description,
            activity.OccurredAtUtc)).ToList();

        return Result<TicketHistoryPage>.Success(new TicketHistoryPage(items, page, pageSize, totalCount));
    }
}
