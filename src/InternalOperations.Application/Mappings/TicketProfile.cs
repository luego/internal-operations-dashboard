using AutoMapper;
using InternalOperations.Application.DTOs;
using InternalOperations.Domain.Tickets;

namespace InternalOperations.Application.Mappings;

public class TicketProfile : Profile
{
    public TicketProfile()
    {
        CreateMap<Ticket, TicketDto>();
        CreateMap<CreateTicketDto, Ticket>();
    }
}