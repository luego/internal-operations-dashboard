using InternalOperations.Domain.Tickets;
using InternalOperations.Persistence.Abstractions;
using InternalOperations.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Persistence.Repositories;

public sealed class TicketRepository
    : GenericRepository<Ticket>, ITicketRepository
{
    public TicketRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<Ticket>> GetAssignedToUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await Entities
            .Include(ticket => ticket.User)
            .Where(ticket => ticket.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Ticket?> GetWithDetailsAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        return await Entities
            .Include(ticket => ticket.User)
            // .Include(ticket => ticket.Department)
            // .Include(ticket => ticket.Comments)
            .FirstOrDefaultAsync(
                ticket => ticket.Id == ticketId,
                cancellationToken);
    }
}