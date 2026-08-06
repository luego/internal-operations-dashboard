using InternalOperations.Application.DTOs;
namespace InternalOperations.Application.Features.Tickets;

public sealed record CreateTicketCommand(
    CreateTicketDto Ticket)
    : IRequest<Result<TicketDto>>;
