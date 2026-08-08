using InternalOperations.Application;
using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Application.Features.Dashboard;
using InternalOperations.Domain.Tickets;
using InternalOperations.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Persistence.Repositories;

public sealed class DashboardQueryService(ApplicationDbContext context, IClock clock) : IDashboardQueryService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var generatedAtUtc = clock.UtcNow.UtcDateTime;
        var ticketCounts = await context.Tickets
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new DashboardSummaryDto(
                generatedAtUtc,
                group.Count(),
                group.Count(ticket => ticket.Status == TicketStatus.Open),
                group.Count(ticket => ticket.Status == TicketStatus.InProgress),
                group.Count(ticket => ticket.Status == TicketStatus.Resolved),
                group.Count(ticket => ticket.Status == TicketStatus.Closed),
                group.Count(ticket => ticket.UserId == null),
                group.Count(ticket =>
                    (ticket.Status == TicketStatus.Open || ticket.Status == TicketStatus.InProgress)
                    && (ticket.Priority == TicketPriority.High || ticket.Priority == TicketPriority.Critical)),
                0,
                0))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new DashboardSummaryDto(generatedAtUtc, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        var activeDepartments = await context.Departments
            .AsNoTracking()
            .CountAsync(department => department.IsActive, cancellationToken);
        var activeUsers = await context.DomainUsers
            .AsNoTracking()
            .CountAsync(user => user.IsActive, cancellationToken);

        return ticketCounts with
        {
            ActiveDepartments = activeDepartments,
            ActiveUsers = activeUsers,
        };
    }

    public async Task<DashboardTrendsDto> GetTrendsAsync(int days, CancellationToken cancellationToken)
    {
        var today = clock.UtcNow.UtcDateTime.Date;
        var start = today.AddDays(-(days - 1));
        var endExclusive = today.AddDays(1);
        var ticketCounts = await context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.CreatedAtUtc >= start && ticket.CreatedAtUtc < endExclusive)
            .GroupBy(ticket => ticket.CreatedAtUtc.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Date, item => item.Count, cancellationToken);
        var commentCounts = await context.TicketComments
            .AsNoTracking()
            .Where(comment => comment.CreatedAtUtc >= start && comment.CreatedAtUtc < endExclusive)
            .GroupBy(comment => comment.CreatedAtUtc.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Date, item => item.Count, cancellationToken);
        var points = Enumerable.Range(0, days)
            .Select(offset => start.AddDays(offset))
            .Select(date => new DashboardTrendPointDto(
                DateOnly.FromDateTime(date),
                ticketCounts.GetValueOrDefault(date),
                commentCounts.GetValueOrDefault(date)))
            .ToArray();

        return new DashboardTrendsDto(
            DateOnly.FromDateTime(start),
            DateOnly.FromDateTime(today),
            points);
    }
}
