using InternalOperations.Persistence.Abstractions;
using InternalOperations.Persistence.Context;

namespace InternalOperations.Persistence;

public sealed class UnitOfWork(
    ApplicationDbContext context,
    ITicketRepository ticketRepository,
    IUserRepository userRepository) : IUnitOfWork
{
    private readonly ApplicationDbContext _context = context;

    public ITicketRepository Tickets { get; } = ticketRepository;

    public IUserRepository Users { get; } = userRepository;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
