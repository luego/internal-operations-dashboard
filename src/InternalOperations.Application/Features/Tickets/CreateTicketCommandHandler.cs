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

public sealed class ListTicketsQueryHandler(ITicketAdministrationService tickets)
    : IRequestHandler<ListTicketsQuery, Result<TicketPage>>
{
    public async Task<Result<TicketPage>> Handle(ListTicketsQuery request, CancellationToken cancellationToken) =>
        Result<TicketPage>.Success(await tickets.ListAsync(
            new TicketListFilter(
                request.Page,
                request.PageSize,
                string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
                request.Status,
                request.Priority,
                request.DepartmentId,
                request.UserId,
                request.SortBy,
                request.SortDirection),
            cancellationToken));
}

public sealed class UpdateTicketCommandHandler(ITicketAdministrationService tickets)
    : IRequestHandler<UpdateTicketCommand, Result<TicketDto>>
{
    public Task<Result<TicketDto>> Handle(UpdateTicketCommand request, CancellationToken cancellationToken) =>
        tickets.UpdateAsync(request, cancellationToken);
}

public sealed class ChangeTicketStatusCommandHandler(ITicketAdministrationService tickets)
    : IRequestHandler<ChangeTicketStatusCommand, Result<TicketDto>>
{
    public Task<Result<TicketDto>> Handle(ChangeTicketStatusCommand request, CancellationToken cancellationToken) =>
        tickets.ChangeStatusAsync(request, cancellationToken);
}
