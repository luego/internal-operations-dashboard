using InternalOperations.Application;
using InternalOperations.Domain.Departments;
using InternalOperations.Domain.Tickets;
using InternalOperations.Domain.Users;
using InternalOperations.Persistence.Context;
using InternalOperations.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Persistence.IntegrationTests;

public sealed class DashboardQueryServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SummaryAndTrendsAggregateOnlyActiveRows()
    {
        await using var context = CreateContext();
        var activeDepartment = Department.Create("Operations", null, Now.UtcDateTime);
        var inactiveDepartment = Department.Create("Archived", null, Now.UtcDateTime);
        inactiveDepartment.Deactivate(Now.UtcDateTime);
        var activeUser = User.Create(Guid.NewGuid(), "agent", "Agent", activeDepartment.Id, Now.UtcDateTime);
        var inactiveUser = User.Create(Guid.NewGuid(), "inactive", "Inactive", null, Now.UtcDateTime);
        inactiveUser.Deactivate(Now.UtcDateTime);

        var open = Ticket.Create("Open", "Open ticket", TicketPriority.High, activeDepartment.Id, null, Now.AddDays(-2).UtcDateTime);
        var inProgress = Ticket.Create("Progress", "In progress ticket", TicketPriority.Critical, activeDepartment.Id, activeUser.Id, Now.AddDays(-2).UtcDateTime);
        Assert.True(inProgress.TryTransitionTo(TicketStatus.InProgress, Now.AddDays(-1).UtcDateTime));
        var resolved = Ticket.Create("Resolved", "Resolved ticket", TicketPriority.Medium, activeDepartment.Id, activeUser.Id, Now.UtcDateTime);
        Assert.True(resolved.TryTransitionTo(TicketStatus.InProgress, Now.UtcDateTime));
        Assert.True(resolved.TryTransitionTo(TicketStatus.Resolved, Now.UtcDateTime));
        var closed = Ticket.Create("Closed", "Closed ticket", TicketPriority.Low, activeDepartment.Id, null, Now.UtcDateTime);
        Assert.True(closed.TryTransitionTo(TicketStatus.Closed, Now.UtcDateTime));
        var deleted = Ticket.Create("Deleted", "Must not count", TicketPriority.Critical, activeDepartment.Id, null, Now.UtcDateTime);
        deleted.Delete();

        var comment = TicketComment.Create(open.Id, activeUser.Id, "Investigating", Now.AddDays(-1).UtcDateTime);
        var deletedComment = TicketComment.Create(open.Id, activeUser.Id, "Deleted comment", Now.UtcDateTime);
        deletedComment.Delete();
        context.AddRange(
            activeDepartment,
            inactiveDepartment,
            activeUser,
            inactiveUser,
            open,
            inProgress,
            resolved,
            closed,
            deleted,
            comment,
            deletedComment);
        await context.SaveChangesAsync();
        var service = new DashboardQueryService(context, new FixedClock());

        var summary = await service.GetSummaryAsync(default);
        var trends = await service.GetTrendsAsync(3, default);

        Assert.Equal(4, summary.TotalTickets);
        Assert.Equal(1, summary.OpenTickets);
        Assert.Equal(1, summary.InProgressTickets);
        Assert.Equal(1, summary.ResolvedTickets);
        Assert.Equal(1, summary.ClosedTickets);
        Assert.Equal(2, summary.UnassignedTickets);
        Assert.Equal(2, summary.HighPriorityActiveTickets);
        Assert.Equal(1, summary.ActiveDepartments);
        Assert.Equal(1, summary.ActiveUsers);
        Assert.Equal(new DateOnly(2026, 8, 6), trends.From);
        Assert.Equal(new DateOnly(2026, 8, 8), trends.To);
        Assert.Equal([2, 0, 2], trends.Points.Select(point => point.TicketsCreated));
        Assert.Equal([0, 1, 0], trends.Points.Select(point => point.CommentsAdded));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
