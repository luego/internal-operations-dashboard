using InternalOperations.Application.Features.Tickets;

namespace InternalOperations.Application.Abstractions.Persistence;

public interface ITicketAdministrationService
{
    Task<Result<TicketDto>> CreateAsync(CreateTicketCommand command, CancellationToken cancellationToken);
    Task<TicketDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<TicketPage> ListAsync(TicketListFilter filter, CancellationToken cancellationToken);
    Task<Result<TicketDto>> UpdateAsync(UpdateTicketCommand command, CancellationToken cancellationToken);
    Task<Result<TicketDto>> ChangeStatusAsync(ChangeTicketStatusCommand command, CancellationToken cancellationToken);
}
