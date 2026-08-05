using InternalOperations.Domain.Tickets;

namespace InternalOperations.Persistence.Abstractions;

public interface ITicketRepository : IRepository<Ticket>
{
    Task<Ticket?> GetWithDetailsAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    // Task<IReadOnlyList<Ticket>> SearchAsync(
    //     TicketSearchCriteria criteria,
    //     CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ticket>> GetAssignedToUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}