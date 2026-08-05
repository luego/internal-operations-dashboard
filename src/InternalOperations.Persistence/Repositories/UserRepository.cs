using InternalOperations.Domain.Users;
using InternalOperations.Persistence.Abstractions;
using InternalOperations.Persistence.Context;

namespace InternalOperations.Persistence.Repositories;

public sealed class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetWithDetailsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}