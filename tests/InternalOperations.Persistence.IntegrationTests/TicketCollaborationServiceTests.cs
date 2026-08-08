using InternalOperations.Application;
using InternalOperations.Application.Features.TicketCollaboration;
using InternalOperations.Application.Features.Tickets;
using InternalOperations.Domain.Departments;
using InternalOperations.Domain.Tickets;
using InternalOperations.Domain.Users;
using InternalOperations.Persistence.Context;
using InternalOperations.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Persistence.IntegrationTests;

public sealed class TicketCollaborationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 8, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddCommentPersistsCommentAndActivityAtomically()
    {
        await using var context = CreateContext();
        var department = Department.Create("Operations", null, Now.UtcDateTime);
        var author = User.Create(Guid.NewGuid(), "agent", "Support Agent", null, Now.UtcDateTime);
        context.Departments.Add(department);
        context.DomainUsers.Add(author);
        await context.SaveChangesAsync();
        var ticket = await new TicketAdministrationService(context, new FixedClock()).CreateAsync(
            new CreateTicketCommand("Printer outage", "Cannot print", TicketPriority.High, department.Id, null),
            default);
        var service = new TicketCollaborationService(context, new FixedClock());

        var result = await service.AddCommentAsync(ticket.Value!.Id, author.Id, "  Restarted\t printer ", default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Restarted printer", result.Value!.Comment);
        Assert.Equal("Support Agent", result.Value.AuthorDisplayName);
        Assert.Single(context.TicketComments);
        var activity = Assert.Single(context.Set<TicketActivity>().Where(item => item.Type == TicketActivityType.CommentAdded));
        Assert.Equal(author.Id, activity.ActorUserId);
    }

    [Fact]
    public async Task ListAndHistoryArePagedAndDeterministicallyOrdered()
    {
        await using var context = CreateContext();
        var department = Department.Create("Operations", null, Now.UtcDateTime);
        var author = User.Create(Guid.NewGuid(), "agent", "Support Agent", null, Now.UtcDateTime);
        var ticket = Ticket.Create("Printer outage", "Cannot print", TicketPriority.Medium, department.Id, null, Now.UtcDateTime);
        context.AddRange(department, author, ticket);
        await context.SaveChangesAsync();
        var firstWriter = new TicketCollaborationService(context, new FixedClock());
        var secondWriter = new TicketCollaborationService(context, new FixedClock(Now.AddSeconds(1)));

        await firstWriter.AddCommentAsync(ticket.Id, author.Id, "First", default);
        await secondWriter.AddCommentAsync(ticket.Id, author.Id, "Second", default);

        var comments = await firstWriter.ListCommentsAsync(ticket.Id, 1, 1, default);
        var history = await firstWriter.GetHistoryAsync(ticket.Id, 1, 10, default);

        Assert.True(comments.IsSuccess);
        Assert.Equal(2, comments.Value!.TotalCount);
        Assert.Equal("First", Assert.Single(comments.Value.Items).Comment);
        Assert.True(history.IsSuccess);
        Assert.Equal(2, history.Value!.TotalCount);
        Assert.All(history.Value.Items, item => Assert.Equal(TicketActivityType.CommentAdded, item.Type));
    }

    [Fact]
    public async Task HistoryRetainsAuthorDisplayAfterLogicalUserDeletion()
    {
        await using var context = CreateContext();
        var department = Department.Create("Operations", null, Now.UtcDateTime);
        var author = User.Create(Guid.NewGuid(), "agent", "Former Agent", null, Now.UtcDateTime);
        var ticket = Ticket.Create("Printer outage", "Cannot print", TicketPriority.Medium, department.Id, null, Now.UtcDateTime);
        context.AddRange(department, author, ticket);
        await context.SaveChangesAsync();
        var service = new TicketCollaborationService(context, new FixedClock());
        await service.AddCommentAsync(ticket.Id, author.Id, "Investigated", default);
        author.Delete();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var comments = await service.ListCommentsAsync(ticket.Id, 1, 25, default);
        var history = await service.GetHistoryAsync(ticket.Id, 1, 25, default);

        Assert.Equal("Former Agent", Assert.Single(comments.Value!.Items).AuthorDisplayName);
        Assert.Equal("Former Agent", Assert.Single(history.Value!.Items).ActorDisplayName);
    }

    [Fact]
    public async Task AddCommentRejectsUnknownAuthorWithoutWritingAnything()
    {
        await using var context = CreateContext();
        var department = Department.Create("Operations", null, Now.UtcDateTime);
        var ticket = Ticket.Create("Printer outage", "Cannot print", TicketPriority.Medium, department.Id, null, Now.UtcDateTime);
        context.AddRange(department, ticket);
        await context.SaveChangesAsync();
        var service = new TicketCollaborationService(context, new FixedClock());

        var result = await service.AddCommentAsync(ticket.Id, Guid.NewGuid(), "Comment", default);

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketCollaborationErrors.AuthorNotFound, result.Error);
        Assert.Empty(context.TicketComments);
        Assert.Empty(context.Set<TicketActivity>());
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class FixedClock(DateTimeOffset? value = null) : IClock
    {
        public DateTimeOffset UtcNow => value ?? Now;
    }
}
