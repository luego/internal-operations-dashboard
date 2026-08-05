using AutoMapper;
using InternalOperations.Application.Abstractions.Services;
using InternalOperations.Application.DTOs;
using InternalOperations.Domain.Tickets;
using InternalOperations.Persistence.Abstractions;

namespace InternalOperations.Application.Services;

public sealed class TicketService : ITicketService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TicketService(
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<Result> AssignAsync(Guid ticketId, Guid userId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Result> CloseAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<TicketDto>> CreateAsync(
        CreateTicketDto dto,
        CancellationToken cancellationToken)
    {
        var ticket = _mapper.Map<Ticket>(dto);

        await _unitOfWork.Tickets.AddAsync(
            ticket,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<TicketDto>.Success(
            _mapper.Map<TicketDto>(ticket));
    }

    public Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Result<TicketDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}