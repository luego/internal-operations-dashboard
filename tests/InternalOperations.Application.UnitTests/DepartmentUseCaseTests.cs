using InternalOperations.Application;
using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Application.Features.Departments;
using InternalOperations.Domain.Departments;

namespace InternalOperations.Application.UnitTests;

public sealed class DepartmentUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 7, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatePersistsCanonicalDepartmentAndReturnsSafeDto()
    {
        var repository = new FakeDepartmentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateDepartmentCommandHandler(repository, unitOfWork, new FakeClock());

        var result = await handler.Handle(new CreateDepartmentCommand("  Customer\tSupport  ", null), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Customer Support", result.Value!.Name);
        Assert.Equal(string.Empty, result.Value.Description);
        Assert.True(result.Value.IsActive);
        Assert.Equal(Now.UtcDateTime, result.Value.CreatedAtUtc);
        Assert.NotNull(repository.Added);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateRejectsNormalizedDuplicateBeforeWrite()
    {
        var repository = new FakeDepartmentRepository { NameExists = true };
        var handler = new CreateDepartmentCommandHandler(repository, new FakeUnitOfWork(), new FakeClock());

        var result = await handler.Handle(new CreateDepartmentCommand("operations", null), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("departments.name_conflict", result.Error!.Code);
        Assert.Null(repository.Added);
    }

    [Fact]
    public async Task GetReturnsStableNotFoundForMissingDepartment()
    {
        var result = await new GetDepartmentQueryHandler(new FakeDepartmentReadService())
            .Handle(new GetDepartmentQuery(Guid.NewGuid()), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("departments.not_found", result.Error!.Code);
    }

    [Fact]
    public void ValidatorRejectsInvalidNameBeforeHandlerRuns()
    {
        var result = new CreateDepartmentCommandValidator().Validate(new CreateDepartmentCommand(" ", null));

        Assert.False(result.IsSuccess);
        Assert.Equal("departments.invalid_name", result.Error!.Code);
    }

    [Fact]
    public async Task ListReturnsPagedProjectionFromReadService()
    {
        var readService = new FakeDepartmentReadService
        {
            Page = new PagedResponse<DepartmentDto>(
                [DepartmentDto.From(Department.Create("Operations", null, Now.UtcDateTime))],
                2,
                10,
                21),
        };

        var result = await new ListDepartmentsQueryHandler(readService)
            .Handle(new ListDepartmentsQuery(2, 10, "ops", true, "name", "desc"), default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(3, result.Value.TotalPages);
        Assert.Equal(2, readService.ReceivedFilter!.Page);
        Assert.Equal("ops", readService.ReceivedFilter.Search);
    }

    [Theory]
    [InlineData(0, 25, "name", "asc")]
    [InlineData(1, 101, "name", "asc")]
    [InlineData(1, 25, "invalid", "asc")]
    [InlineData(1, 25, "name", "sideways")]
    public void ListValidatorRejectsInvalidPagingOrSort(int page, int pageSize, string sortBy, string direction)
    {
        var result = new ListDepartmentsQueryValidator()
            .Validate(new ListDepartmentsQuery(page, pageSize, null, null, sortBy, direction));

        Assert.False(result.IsSuccess);
        Assert.Equal("departments.invalid_list", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateRejectsStaleVersionWithoutSaving()
    {
        var department = Department.Create("Operations", null);
        var repository = new FakeDepartmentRepository { Tracked = department };
        var unitOfWork = new FakeUnitOfWork();

        var result = await new UpdateDepartmentCommandHandler(repository, unitOfWork, new FakeClock())
            .Handle(new UpdateDepartmentCommand(department.Id, "Service", null, Guid.NewGuid()), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("departments.version_conflict", result.Error!.Code);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateChangesDepartmentAndMapsUniqueRace()
    {
        var department = Department.Create("Operations", null);
        var repository = new FakeDepartmentRepository { Tracked = department };
        var unitOfWork = new FakeUnitOfWork { Exception = new PersistenceUniqueConstraintException() };

        var result = await new UpdateDepartmentCommandHandler(repository, unitOfWork, new FakeClock())
            .Handle(new UpdateDepartmentCommand(department.Id, "Service", null, department.Version), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("departments.name_conflict", result.Error!.Code);
    }

    [Fact]
    public async Task DeactivateRejectsDepartmentWithActiveWork()
    {
        var department = Department.Create("Operations", null);
        var repository = new FakeDepartmentRepository { Tracked = department, HasActiveWork = true };

        var result = await new SetDepartmentStatusCommandHandler(repository, new FakeUnitOfWork(), new FakeClock())
            .Handle(new SetDepartmentStatusCommand(department.Id, false, department.Version), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("departments.active_work_conflict", result.Error!.Code);
        Assert.True(department.IsActive);
    }

    [Fact]
    public async Task StatusChangeIsIdempotentAndReturnsCurrentDto()
    {
        var department = Department.Create("Operations", null);
        department.Deactivate(Now.UtcDateTime.AddMinutes(-1));
        var repository = new FakeDepartmentRepository { Tracked = department };
        var unitOfWork = new FakeUnitOfWork();

        var result = await new SetDepartmentStatusCommandHandler(repository, unitOfWork, new FakeClock())
            .Handle(new SetDepartmentStatusCommand(department.Id, false, department.Version), default);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsActive);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private sealed class FakeDepartmentRepository : IDepartmentRepository
    {
        public bool NameExists { get; set; }
        public bool HasActiveWork { get; set; }
        public Department? Tracked { get; set; }
        public Department? Added { get; private set; }

        public Task<Department?> GetTrackedAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Tracked);
        public Task<bool> NormalizedNameExistsAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken) => Task.FromResult(NameExists);
        public Task<bool> HasActiveWorkAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(HasActiveWork);
        public Task AddAsync(Department department, CancellationToken cancellationToken)
        {
            Added = department;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDepartmentReadService : IDepartmentReadService
    {
        public DepartmentListFilter? ReceivedFilter { get; private set; }
        public PagedResponse<DepartmentDto> Page { get; set; } = new([], 1, 25, 0);
        public Task<DepartmentDto?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<DepartmentDto?>(null);
        public Task<PagedResponse<DepartmentDto>> ListAsync(DepartmentListFilter filter, CancellationToken cancellationToken)
        {
            ReceivedFilter = filter;
            return Task.FromResult(Page);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Exception? Exception { get; set; }
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (Exception is not null)
            {
                throw Exception;
            }

            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
