using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Application.Features.Tickets;
using InternalOperations.Domain.Tickets;

namespace InternalOperations.Application.UnitTests;

public sealed class TicketUseCaseTests
{
    [Fact]
    public void CreateValidatorRejectsInvalidData()
    {
        var command = new CreateTicketCommand(" ", "Description", TicketPriority.Medium, Guid.Empty, null);

        var result = new CreateTicketCommandValidator().Validate(command);

        Assert.False(result.IsSuccess);
        Assert.Equal("tickets.invalid_request", result.Error!.Code);
    }

    [Fact]
    public async Task CreateHandlerReturnsTicketFromAdministrationPort()
    {
        var departmentId = Guid.NewGuid();
        var service = new FakeTicketAdministrationService
        {
            Created = TicketDtoFor(departmentId),
        };
        var command = new CreateTicketCommand("Printer outage", "Cannot print", TicketPriority.High, departmentId, null);

        var result = await new CreateTicketCommandHandler(service).Handle(command, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value!.Number);
        Assert.Same(command, service.ReceivedCreate);
    }

    [Fact]
    public async Task GetHandlerReturnsStableNotFound()
    {
        var result = await new GetTicketQueryHandler(new FakeTicketAdministrationService())
            .Handle(new GetTicketQuery(Guid.NewGuid()), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("tickets.not_found", result.Error!.Code);
    }

    private static TicketDto TicketDtoFor(Guid departmentId) => new(
        Guid.NewGuid(),
        42,
        "Printer outage",
        "Cannot print",
        TicketStatus.Open,
        TicketPriority.High,
        new TicketDepartmentDto(departmentId, "Operations"),
        null,
        new DateTime(2026, 8, 7, 22, 0, 0, DateTimeKind.Utc),
        null,
        Guid.NewGuid());

    private sealed class FakeTicketAdministrationService : ITicketAdministrationService
    {
        public TicketDto? Created { get; set; }
        public CreateTicketCommand? ReceivedCreate { get; private set; }

        public Task<Result<TicketDto>> CreateAsync(CreateTicketCommand command, CancellationToken cancellationToken)
        {
            ReceivedCreate = command;
            return Task.FromResult(Result<TicketDto>.Success(Created!));
        }

        public Task<TicketDto?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<TicketDto?>(null);
    }
}
