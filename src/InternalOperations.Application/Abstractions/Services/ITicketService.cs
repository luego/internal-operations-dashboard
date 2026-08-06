using InternalOperations.Application.DTOs;

namespace InternalOperations.Application.Abstractions.Services;

public interface ITicketService
{
    Task<Result<TicketDto>> CreateAsync(
        CreateTicketDto dto,
        CancellationToken cancellationToken);
}
