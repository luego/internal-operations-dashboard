using InternalOperations.Domain.Entities;

namespace InternalOperations.Persistence.Abstractions;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetWithDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    // Task<IReadOnlyList<User>> SearchAsync(
    //     UserSearchCriteria criteria,
    //     CancellationToken cancellationToken = default);
}