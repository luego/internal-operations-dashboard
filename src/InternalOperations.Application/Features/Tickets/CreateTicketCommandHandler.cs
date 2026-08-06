using InternalOperations.Application.Abstractions.Services;
using InternalOperations.Application.DTOs;

namespace InternalOperations.Application.Features.Tickets;

public sealed class CreateTicketCommandHandler
    : IRequestHandler<CreateTicketCommand, Result<TicketDto>>
{
    private readonly ITicketService _ticketService;

    public CreateTicketCommandHandler(
        ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    public Task<Result<TicketDto>> Handle(
        CreateTicketCommand request,
        CancellationToken cancellationToken)
    {
        return _ticketService.CreateAsync(
            request.Ticket,
            cancellationToken);
    }
}
