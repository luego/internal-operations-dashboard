using InternalOperations.Application;
using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Application.Features.Users;

namespace InternalOperations.Application.UnitTests;

public sealed class UserUseCaseTests
{
    [Fact]
    public async Task CreateDelegatesSafeAdministrativeRequest()
    {
        var service = new FakeUserAdministrationService();
        var command = new CreateUserCommand(
            "agent.one",
            "agent@example.test",
            "Agent One",
            "Valid!Password123",
            ["Agent"],
            null);

        var result = await new CreateUserCommandHandler(service).Handle(command, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(command, service.Created);
        Assert.DoesNotContain("Password", string.Join(',', typeof(UserDto).GetProperties().Select(x => x.Name)), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetReturnsStableNotFound()
    {
        var result = await new GetUserQueryHandler(new FakeUserAdministrationService())
            .Handle(new GetUserQuery(Guid.NewGuid()), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("users.not_found", result.Error!.Code);
    }

    [Theory]
    [InlineData(0, 25, "userName", "asc")]
    [InlineData(1, 101, "userName", "asc")]
    [InlineData(1, 25, "invalid", "asc")]
    [InlineData(1, 25, "email", "sideways")]
    public void ListValidatorRejectsInvalidPagingAndSort(int page, int pageSize, string sortBy, string direction)
    {
        var result = new ListUsersQueryValidator().Validate(new ListUsersQuery(
            page, pageSize, null, null, null, null, null, sortBy, direction));

        Assert.False(result.IsSuccess);
        Assert.Equal("users.invalid_list", result.Error!.Code);
    }

    [Fact]
    public void CreateValidatorRejectsUnknownOrDuplicateRoles()
    {
        var validator = new CreateUserCommandValidator();

        var unknown = validator.Validate(new CreateUserCommand("u", "u@example.test", "User", "Password!123", ["Owner"], null));
        var duplicate = validator.Validate(new CreateUserCommand("u", "u@example.test", "User", "Password!123", ["Agent", "Agent"], null));

        Assert.False(unknown.IsSuccess);
        Assert.False(duplicate.IsSuccess);
        Assert.Equal("users.invalid_roles", unknown.Error!.Code);
        Assert.Equal("users.invalid_roles", duplicate.Error!.Code);
    }

    [Fact]
    public async Task MutationsDelegateToAtomicAdministrationPort()
    {
        var service = new FakeUserAdministrationService();
        var id = Guid.NewGuid();
        var version = Guid.NewGuid();

        await new UpdateUserCommandHandler(service).Handle(new(id, "agent", "agent@example.test", "Agent", version), default);
        await new SetUserDepartmentCommandHandler(service).Handle(new(id, null, version), default);
        await new SetUserStatusCommandHandler(service).Handle(new(id, false, version), default);
        await new SetUserRolesCommandHandler(service).Handle(new(id, ["Viewer"], version), default);

        Assert.Equal(4, service.MutationCount);
    }

    private sealed class FakeUserAdministrationService : IUserAdministrationService
    {
        public CreateUserCommand? Created { get; private set; }
        public int MutationCount { get; private set; }

        public Task<Result<UserDto>> CreateAsync(CreateUserCommand command, CancellationToken cancellationToken)
        {
            Created = command;
            return Task.FromResult(Result<UserDto>.Success(Sample()));
        }

        public Task<UserDto?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<UserDto?>(null);
        public Task<UserPage> ListAsync(UserListFilter filter, CancellationToken cancellationToken) =>
            Task.FromResult(new UserPage([], filter.Page, filter.PageSize, 0));
        public Task<Result<UserDto>> UpdateAsync(UpdateUserCommand command, CancellationToken cancellationToken) => Mutated();
        public Task<Result<UserDto>> SetDepartmentAsync(SetUserDepartmentCommand command, CancellationToken cancellationToken) => Mutated();
        public Task<Result<UserDto>> SetStatusAsync(SetUserStatusCommand command, CancellationToken cancellationToken) => Mutated();
        public Task<Result<UserDto>> SetRolesAsync(SetUserRolesCommand command, CancellationToken cancellationToken) => Mutated();

        private Task<Result<UserDto>> Mutated()
        {
            MutationCount++;
            return Task.FromResult(Result<UserDto>.Success(Sample()));
        }

        private static UserDto Sample() => new(
            Guid.NewGuid(), "agent", "agent@example.test", "Agent", true, null, ["Agent"], DateTime.UtcNow, null, Guid.NewGuid());
    }
}
