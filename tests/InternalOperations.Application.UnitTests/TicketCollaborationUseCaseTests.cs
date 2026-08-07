using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Application.Features.TicketCollaboration;
using InternalOperations.Domain.Tickets;

namespace InternalOperations.Application.UnitTests;

public sealed class TicketCollaborationUseCaseTests
{
    [Fact]
    public void AddCommentValidatorRejectsBlankOrOversizedBody()
    {
        var validator = new AddTicketCommentCommandValidator();

        Assert.False(validator.Validate(new AddTicketCommentCommand(Guid.NewGuid(), " ")).IsSuccess);
        Assert.False(validator.Validate(new AddTicketCommentCommand(Guid.NewGuid(), new string('C', 4001))).IsSuccess);
        Assert.False(validator.Validate(new AddTicketCommentCommand(Guid.Empty, "Comment")).IsSuccess);
    }

    [Fact]
    public async Task AddCommentHandlerUsesAuthenticatedUserInsteadOfClientInput()
    {
        var authorId = Guid.NewGuid();
        var service = new RecordingCollaborationService();
        var handler = new AddTicketCommentCommandHandler(service, new StubCurrentUser(authorId));
        var command = new AddTicketCommentCommand(Guid.NewGuid(), "Comment");

        var result = await handler.Handle(command, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(authorId, service.AuthorId);
        Assert.Equal(command.TicketId, service.TicketId);
    }

    [Fact]
    public async Task AddCommentHandlerRejectsMissingAuthenticatedUser()
    {
        var handler = new AddTicketCommentCommandHandler(new RecordingCollaborationService(), new StubCurrentUser(null));

        var result = await handler.Handle(new AddTicketCommentCommand(Guid.NewGuid(), "Comment"), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(TicketCollaborationErrors.AuthorRequired, result.Error);
    }

    private sealed class StubCurrentUser(Guid? userId) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;
        public string? UserName => null;
    }

    private sealed class RecordingCollaborationService : ITicketCollaborationService
    {
        public Guid? AuthorId { get; private set; }
        public Guid? TicketId { get; private set; }

        public Task<Result<TicketCommentDto>> AddCommentAsync(Guid ticketId, Guid authorId, string comment, CancellationToken cancellationToken)
        {
            TicketId = ticketId;
            AuthorId = authorId;
            return Task.FromResult(Result<TicketCommentDto>.Success(new TicketCommentDto(
                Guid.NewGuid(), ticketId, authorId, "Author", comment, DateTime.UtcNow)));
        }

        public Task<Result<TicketCommentPage>> ListCommentsAsync(Guid ticketId, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(Result<TicketCommentPage>.Success(new TicketCommentPage([], page, pageSize, 0)));

        public Task<Result<TicketHistoryPage>> GetHistoryAsync(Guid ticketId, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(Result<TicketHistoryPage>.Success(new TicketHistoryPage([], page, pageSize, 0)));
    }
}
