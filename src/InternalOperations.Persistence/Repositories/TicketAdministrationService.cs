using InternalOperations.Application;
using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Application.Features.Tickets;
using InternalOperations.Domain.Tickets;
using InternalOperations.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Persistence.Repositories;

public sealed class TicketAdministrationService(ApplicationDbContext context, IClock clock)
    : ITicketAdministrationService
{
    public async Task<Result<TicketDto>> CreateAsync(
        CreateTicketCommand command,
        CancellationToken cancellationToken)
    {
        var department = await context.Departments
            .AsNoTracking()
            .Where(item => item.Id == command.DepartmentId)
            .Select(item => new { item.Id, item.Name, item.IsActive })
            .SingleOrDefaultAsync(cancellationToken);
        if (department is null)
        {
            return Result<TicketDto>.Failure(TicketErrors.DepartmentNotFound);
        }

        if (!department.IsActive)
        {
            return Result<TicketDto>.Failure(TicketErrors.DepartmentInactive);
        }

        TicketAssigneeDto? assignee = null;
        if (command.UserId.HasValue)
        {
            var user = await context.DomainUsers
                .AsNoTracking()
                .Where(item => item.Id == command.UserId.Value)
                .Select(item => new { item.Id, item.DisplayName, item.IsActive })
                .SingleOrDefaultAsync(cancellationToken);
            if (user is null)
            {
                return Result<TicketDto>.Failure(TicketErrors.UserNotFound);
            }

            if (!user.IsActive)
            {
                return Result<TicketDto>.Failure(TicketErrors.UserInactive);
            }

            assignee = new TicketAssigneeDto(user.Id, user.DisplayName);
        }

        Ticket ticket;
        try
        {
            ticket = Ticket.Create(
                command.Title,
                command.Description,
                command.Priority,
                command.DepartmentId,
                command.UserId,
                clock.UtcNow.UtcDateTime);
        }
        catch (ArgumentException)
        {
            return Result<TicketDto>.Failure(TicketErrors.InvalidRequest);
        }

        await context.Tickets.AddAsync(ticket, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return Result<TicketDto>.Success(ToDto(
            ticket,
            new TicketDepartmentDto(department.Id, department.Name),
            assignee));
    }

    public Task<TicketDto?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == id)
            .Select(ticket => new TicketDto(
                ticket.Id,
                ticket.Number,
                ticket.Title,
                ticket.Description,
                ticket.Status,
                ticket.Priority,
                new TicketDepartmentDto(ticket.DepartmentId!.Value, ticket.Department!.Name),
                ticket.UserId.HasValue
                    ? new TicketAssigneeDto(ticket.UserId.Value, ticket.User!.DisplayName)
                    : null,
                ticket.CreatedAtUtc,
                ticket.UpdatedAtUtc,
                ticket.Version))
            .SingleOrDefaultAsync(cancellationToken);

    private static TicketDto ToDto(
        Ticket ticket,
        TicketDepartmentDto department,
        TicketAssigneeDto? assignee) => new(
            ticket.Id,
            ticket.Number,
            ticket.Title,
            ticket.Description,
            ticket.Status,
            ticket.Priority,
            department,
            assignee,
            ticket.CreatedAtUtc,
            ticket.UpdatedAtUtc,
            ticket.Version);
}
