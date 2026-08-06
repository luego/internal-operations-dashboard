using AutoMapper;
using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Application.Abstractions.Services;
using InternalOperations.Application.DTOs;
using InternalOperations.Domain.Tickets;

namespace InternalOperations.Application.Services;

public sealed class TicketService : ITicketService
{
    private readonly IRepository<Ticket> _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TicketService(
        IRepository<Ticket> ticketRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<TicketDto>> CreateAsync(
        CreateTicketDto dto,
        CancellationToken cancellationToken)
    {
        var ticket = _mapper.Map<Ticket>(dto);

        await _ticketRepository.AddAsync(
            ticket,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<TicketDto>.Success(
            _mapper.Map<TicketDto>(ticket));
    }

}
