using InternalOperations.Domain.Tickets;

namespace InternalOperations.Domain.UnitTests;

public sealed class TicketCollaborationTests
{
    private static readonly DateTime OccurredAtUtc = new(2026, 8, 7, 23, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CommentCreateCanonicalizesBodyAndInitializesIdentity()
    {
        var ticketId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        var comment = TicketComment.Create(ticketId, authorId, "  Printer\t restarted  ", OccurredAtUtc);

        Assert.Equal(ticketId, comment.TicketId);
        Assert.Equal(authorId, comment.UserId);
        Assert.Equal("Printer restarted", comment.Comment);
        Assert.Equal(OccurredAtUtc, comment.CreatedAtUtc);
        Assert.NotEqual(Guid.Empty, comment.Id);
    }

    [Fact]
    public void CommentCreateRejectsInvalidInput()
    {
        Assert.Throws<ArgumentException>(() => TicketComment.Create(Guid.Empty, Guid.NewGuid(), "Comment", OccurredAtUtc));
        Assert.Throws<ArgumentException>(() => TicketComment.Create(Guid.NewGuid(), Guid.Empty, "Comment", OccurredAtUtc));
        Assert.Throws<ArgumentException>(() => TicketComment.Create(Guid.NewGuid(), Guid.NewGuid(), " ", OccurredAtUtc));
        Assert.Throws<ArgumentException>(() => TicketComment.Create(Guid.NewGuid(), Guid.NewGuid(), new string('C', 4001), OccurredAtUtc));
        Assert.Throws<ArgumentException>(() => TicketComment.Create(Guid.NewGuid(), Guid.NewGuid(), "Comment", DateTime.SpecifyKind(OccurredAtUtc, DateTimeKind.Local)));
    }

    [Fact]
    public void ActivityCreatePreservesImmutableEventData()
    {
        var ticketId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var activity = TicketActivity.Create(ticketId, actorId, TicketActivityType.CommentAdded, "  Comment\t added ", OccurredAtUtc);

        Assert.Equal(ticketId, activity.TicketId);
        Assert.Equal(actorId, activity.ActorUserId);
        Assert.Equal(TicketActivityType.CommentAdded, activity.Type);
        Assert.Equal("Comment added", activity.Description);
        Assert.Equal(OccurredAtUtc, activity.OccurredAtUtc);
        Assert.NotEqual(Guid.Empty, activity.Id);
    }
}
