using InternalOperations.Domain.Tickets;

namespace InternalOperations.Domain.UnitTests;

public sealed class TicketTests
{
    private static readonly DateTime CreatedAtUtc = new(2026, 8, 7, 21, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateCanonicalizesValuesAndInitializesOperationalState()
    {
        var departmentId = Guid.NewGuid();
        var ticket = Ticket.Create("  Printer\t outage ", "  Cannot   print invoices ", TicketPriority.High, departmentId, null, CreatedAtUtc);

        Assert.Equal("Printer outage", ticket.Title);
        Assert.Equal("Cannot print invoices", ticket.Description);
        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Equal(TicketPriority.High, ticket.Priority);
        Assert.Equal(departmentId, ticket.DepartmentId);
        Assert.Null(ticket.UserId);
        Assert.Equal(CreatedAtUtc, ticket.CreatedAtUtc);
        Assert.NotEqual(Guid.Empty, ticket.Id);
        Assert.NotEqual(Guid.Empty, ticket.Version);
    }

    [Fact]
    public void CreateRejectsInvalidValues()
    {
        Assert.Throws<ArgumentException>(() => Ticket.Create(" ", "Description", TicketPriority.Medium, Guid.NewGuid(), null, CreatedAtUtc));
        Assert.Throws<ArgumentException>(() => Ticket.Create("Title", " ", TicketPriority.Medium, Guid.NewGuid(), null, CreatedAtUtc));
        Assert.Throws<ArgumentException>(() => Ticket.Create(new string('T', 201), "Description", TicketPriority.Medium, Guid.NewGuid(), null, CreatedAtUtc));
        Assert.Throws<ArgumentException>(() => Ticket.Create("Title", new string('D', 4001), TicketPriority.Medium, Guid.NewGuid(), null, CreatedAtUtc));
        Assert.Throws<ArgumentException>(() => Ticket.Create("Title", "Description", TicketPriority.Medium, Guid.Empty, null, CreatedAtUtc));
    }

    [Fact]
    public void UpdateDetailsRotatesVersionAndSameValuesAreIdempotent()
    {
        var ticket = Ticket.Create("Title", "Description", TicketPriority.Low, Guid.NewGuid(), null, CreatedAtUtc);
        var originalVersion = ticket.Version;
        var updatedAt = CreatedAtUtc.AddMinutes(1);

        ticket.UpdateDetails(" Updated  title ", " Updated description ", TicketPriority.Critical, ticket.DepartmentId!.Value, Guid.NewGuid(), updatedAt);
        var updatedVersion = ticket.Version;
        ticket.UpdateDetails("Updated title", "Updated description", TicketPriority.Critical, ticket.DepartmentId.Value, ticket.UserId, updatedAt.AddMinutes(1));

        Assert.Equal("Updated title", ticket.Title);
        Assert.Equal(TicketPriority.Critical, ticket.Priority);
        Assert.NotEqual(originalVersion, updatedVersion);
        Assert.Equal(updatedVersion, ticket.Version);
        Assert.Equal(updatedAt, ticket.UpdatedAtUtc);
    }

    [Fact]
    public void TransitionEnforcesStateMachineAndIsIdempotent()
    {
        var ticket = Ticket.Create("Title", "Description", TicketPriority.Medium, Guid.NewGuid(), null, CreatedAtUtc);
        var originalVersion = ticket.Version;

        Assert.True(ticket.TryTransitionTo(TicketStatus.InProgress, CreatedAtUtc.AddMinutes(1)));
        var inProgressVersion = ticket.Version;
        Assert.True(ticket.TryTransitionTo(TicketStatus.InProgress, CreatedAtUtc.AddMinutes(2)));
        Assert.Equal(inProgressVersion, ticket.Version);
        Assert.True(ticket.TryTransitionTo(TicketStatus.Resolved, CreatedAtUtc.AddMinutes(3)));
        Assert.False(ticket.TryTransitionTo(TicketStatus.Open, CreatedAtUtc.AddMinutes(4)));
        Assert.True(ticket.TryTransitionTo(TicketStatus.Closed, CreatedAtUtc.AddMinutes(5)));
        Assert.False(ticket.TryTransitionTo(TicketStatus.InProgress, CreatedAtUtc.AddMinutes(6)));
        Assert.NotEqual(originalVersion, ticket.Version);
    }
}
