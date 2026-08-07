using InternalOperations.Application.Features.Users;

namespace InternalOperations.Application.Abstractions.Persistence;

public interface IUserAdministrationService
{
    Task<Result<UserDto>> CreateAsync(CreateUserCommand command, CancellationToken cancellationToken);
    Task<UserDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<UserPage> ListAsync(UserListFilter filter, CancellationToken cancellationToken);
    Task<Result<UserDto>> UpdateAsync(UpdateUserCommand command, CancellationToken cancellationToken);
    Task<Result<UserDto>> SetDepartmentAsync(SetUserDepartmentCommand command, CancellationToken cancellationToken);
    Task<Result<UserDto>> SetStatusAsync(SetUserStatusCommand command, CancellationToken cancellationToken);
    Task<Result<UserDto>> SetRolesAsync(SetUserRolesCommand command, CancellationToken cancellationToken);
}
