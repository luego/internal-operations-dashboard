using InternalOperations.Application.DTOs;

namespace InternalOperations.Application.Abstractions.Services;

public interface ITicketService
{
    Task<Result<TicketDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    // Task<Result<PaginatedResult<TicketListDto>>> SearchAsync(
    //     TicketSearchRequest request,
    //     CancellationToken cancellationToken);

    Task<Result<TicketDto>> CreateAsync(
        CreateTicketDto dto,
        CancellationToken cancellationToken);

    // Task<Result<TicketDto>> UpdateAsync(
    //     Guid id,
    //     UpdateTicketDto dto,
    //     CancellationToken cancellationToken);

    Task<Result> AssignAsync(
        Guid ticketId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<Result> CloseAsync(
        Guid ticketId,
        CancellationToken cancellationToken);

    Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken);
}