using InternalOperations.Domain.Tickets;
using InternalOperations.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Persistence.IntegrationTests;

public sealed class UnitOfWorkTests
{
    [Fact]
    public async Task UnitOfWorkCommitsTrackedChanges()
    {
        var options = new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new TestApplicationDbContext(options);
        var unitOfWork = new UnitOfWork(context);

        await context.Tickets.AddAsync(Ticket.Create(
            "Alpha",
            "Unit of work persistence test",
            TicketPriority.Medium,
            Guid.NewGuid(),
            null));
        await unitOfWork.SaveChangesAsync();

        var stored = await context.Tickets.SingleAsync();
        Assert.Equal("Alpha", stored.Title);
    }

    private sealed class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<TestApplicationDbContext> options)
            : base(options)
        {
        }

    }
}
