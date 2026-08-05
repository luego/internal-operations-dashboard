namespace InternalOperations.Persistence.Abstractions;

public interface IUnitOfWork
{
    public ITicketRepository Tickets { get; }
    public IUserRepository Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
