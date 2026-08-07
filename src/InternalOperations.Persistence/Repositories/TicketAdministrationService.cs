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
        var now = clock.UtcNow.UtcDateTime;
        try
        {
            ticket = Ticket.Create(
                command.Title,
                command.Description,
                command.Priority,
                command.DepartmentId,
                command.UserId,
                now);
        }
        catch (ArgumentException)
        {
            return Result<TicketDto>.Failure(TicketErrors.InvalidRequest);
        }

        await context.Tickets.AddAsync(ticket, cancellationToken);
        await context.TicketActivities.AddAsync(
            TicketActivity.Create(ticket.Id, null, TicketActivityType.Created, "Ticket created", now),
            cancellationToken);
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

    public async Task<TicketPage> ListAsync(TicketListFilter filter, CancellationToken cancellationToken)
    {
        var query = context.Tickets.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{filter.Search.Trim()}%";
            query = query.Where(ticket =>
                EF.Functions.Like(ticket.Title, pattern) || EF.Functions.Like(ticket.Description, pattern));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(ticket => ticket.Status == filter.Status.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(ticket => ticket.Priority == filter.Priority.Value);
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(ticket => ticket.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.UserId.HasValue)
        {
            query = query.Where(ticket => ticket.UserId == filter.UserId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var descending = filter.SortDirection == "desc";
        query = filter.SortBy.ToLowerInvariant() switch
        {
            "number" => descending
                ? query.OrderByDescending(ticket => ticket.Number)
                : query.OrderBy(ticket => ticket.Number),
            "updatedatutc" => descending
                ? query.OrderByDescending(ticket => ticket.UpdatedAtUtc).ThenByDescending(ticket => ticket.Number)
                : query.OrderBy(ticket => ticket.UpdatedAtUtc).ThenBy(ticket => ticket.Number),
            "priority" => descending
                ? query.OrderByDescending(ticket => ticket.Priority).ThenByDescending(ticket => ticket.Number)
                : query.OrderBy(ticket => ticket.Priority).ThenBy(ticket => ticket.Number),
            "status" => descending
                ? query.OrderByDescending(ticket => ticket.Status).ThenByDescending(ticket => ticket.Number)
                : query.OrderBy(ticket => ticket.Status).ThenBy(ticket => ticket.Number),
            _ => descending
                ? query.OrderByDescending(ticket => ticket.CreatedAtUtc).ThenByDescending(ticket => ticket.Number)
                : query.OrderBy(ticket => ticket.CreatedAtUtc).ThenBy(ticket => ticket.Number),
        };
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(Project())
            .ToListAsync(cancellationToken);
        return new TicketPage(items, filter.Page, filter.PageSize, totalCount);
    }

    public async Task<Result<TicketDto>> UpdateAsync(
        UpdateTicketCommand command,
        CancellationToken cancellationToken)
    {
        var ticket = await context.Tickets.SingleOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (ticket is null)
        {
            return Result<TicketDto>.Failure(TicketErrors.NotFound);
        }

        if (ticket.Version != command.Version)
        {
            return Result<TicketDto>.Failure(TicketErrors.VersionConflict);
        }

        var references = await ResolveReferencesAsync(command.DepartmentId, command.UserId, cancellationToken);
        if (!references.IsSuccess)
        {
            return Result<TicketDto>.Failure(references.Error!);
        }

        try
        {
            var previousVersion = ticket.Version;
            var now = clock.UtcNow.UtcDateTime;
            ticket.UpdateDetails(
                command.Title,
                command.Description,
                command.Priority,
                command.DepartmentId,
                command.UserId,
                now);
            if (ticket.Version != previousVersion)
            {
                context.TicketActivities.Add(TicketActivity.Create(
                    ticket.Id,
                    null,
                    TicketActivityType.Updated,
                    "Ticket details updated",
                    now));
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (ArgumentException)
        {
            return Result<TicketDto>.Failure(TicketErrors.InvalidRequest);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<TicketDto>.Failure(TicketErrors.VersionConflict);
        }

        return Result<TicketDto>.Success(ToDto(ticket, references.Value!.Department, references.Value.Assignee));
    }

    public async Task<Result<TicketDto>> ChangeStatusAsync(
        ChangeTicketStatusCommand command,
        CancellationToken cancellationToken)
    {
        var ticket = await context.Tickets.SingleOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (ticket is null)
        {
            return Result<TicketDto>.Failure(TicketErrors.NotFound);
        }

        if (ticket.Version != command.Version)
        {
            return Result<TicketDto>.Failure(TicketErrors.VersionConflict);
        }

        var previousStatus = ticket.Status;
        var previousVersion = ticket.Version;
        var now = clock.UtcNow.UtcDateTime;
        if (!ticket.TryTransitionTo(command.Status, now))
        {
            return Result<TicketDto>.Failure(TicketErrors.InvalidTransition);
        }

        if (ticket.Version != previousVersion)
        {
            context.TicketActivities.Add(TicketActivity.Create(
                ticket.Id,
                null,
                TicketActivityType.StatusChanged,
                $"Status changed from {previousStatus} to {command.Status}",
                now));
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<TicketDto>.Failure(TicketErrors.VersionConflict);
        }

        return Result<TicketDto>.Success((await GetAsync(ticket.Id, cancellationToken))!);
    }

    private async Task<Result<TicketReferences>> ResolveReferencesAsync(
        Guid departmentId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var department = await context.Departments
            .AsNoTracking()
            .Where(item => item.Id == departmentId)
            .Select(item => new { item.Id, item.Name, item.IsActive })
            .SingleOrDefaultAsync(cancellationToken);
        if (department is null)
        {
            return Result<TicketReferences>.Failure(TicketErrors.DepartmentNotFound);
        }

        if (!department.IsActive)
        {
            return Result<TicketReferences>.Failure(TicketErrors.DepartmentInactive);
        }

        TicketAssigneeDto? assignee = null;
        if (userId.HasValue)
        {
            var user = await context.DomainUsers
                .AsNoTracking()
                .Where(item => item.Id == userId.Value)
                .Select(item => new { item.Id, item.DisplayName, item.IsActive })
                .SingleOrDefaultAsync(cancellationToken);
            if (user is null)
            {
                return Result<TicketReferences>.Failure(TicketErrors.UserNotFound);
            }

            if (!user.IsActive)
            {
                return Result<TicketReferences>.Failure(TicketErrors.UserInactive);
            }

            assignee = new TicketAssigneeDto(user.Id, user.DisplayName);
        }

        return Result<TicketReferences>.Success(new TicketReferences(
            new TicketDepartmentDto(department.Id, department.Name),
            assignee));
    }

    private static System.Linq.Expressions.Expression<Func<Ticket, TicketDto>> Project() =>
        ticket => new TicketDto(
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
            ticket.Version);

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

    private sealed record TicketReferences(TicketDepartmentDto Department, TicketAssigneeDto? Assignee);
}
