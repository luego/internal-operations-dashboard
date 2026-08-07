using InternalOperations.Application.Abstractions.Persistence;

namespace InternalOperations.Application.Features.Tickets;

public sealed class CreateTicketCommandHandler(ITicketAdministrationService tickets)
    : IRequestHandler<CreateTicketCommand, Result<TicketDto>>
{
    public Task<Result<TicketDto>> Handle(CreateTicketCommand request, CancellationToken cancellationToken) =>
        tickets.CreateAsync(request, cancellationToken);
}

public sealed class GetTicketQueryHandler(ITicketAdministrationService tickets)
    : IRequestHandler<GetTicketQuery, Result<TicketDto>>
{
    public async Task<Result<TicketDto>> Handle(GetTicketQuery request, CancellationToken cancellationToken)
    {
        var ticket = await tickets.GetAsync(request.Id, cancellationToken);
        return ticket is null
            ? Result<TicketDto>.Failure(TicketErrors.NotFound)
            : Result<TicketDto>.Success(ticket);
    }
}
